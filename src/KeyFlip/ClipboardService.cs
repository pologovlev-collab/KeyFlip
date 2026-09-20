using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

public sealed record ClipboardCopyResult(string? Text, uint SequenceBefore, uint SequenceAfter, bool SequenceChanged);

public sealed class ClipboardService
{
    private const int ClipboardTimeoutMilliseconds = 900;
    private const int ClipboardPollIntervalMilliseconds = 15;

    internal Task<ClipboardSnapshot?> CaptureSnapshotAsync(DiagnosticLogger logger, CancellationToken cancellationToken) =>
        CaptureSnapshotWithRetriesAsync(
            Clipboard.GetDataObject,
            logger,
            token => Task.Delay(20, token),
            cancellationToken);

    internal static async Task<ClipboardSnapshot?> CaptureSnapshotWithRetriesAsync(
        Func<IDataObject?> getDataObject,
        DiagnosticLogger logger,
        Func<CancellationToken, Task> delay,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var source = getDataObject();
                var capture = ClipboardSnapshot.Capture(source);
                LogSnapshotCapture(capture, logger);
                if (capture.HasTransientFailure && attempt < 4)
                {
                    capture.Snapshot?.Dispose();
                    logger.Log("CLIPBOARD_SNAPSHOT_RETRY", $"attempt={attempt + 1} reason=transient_format_failure");
                    await delay(cancellationToken);
                    continue;
                }

                if (!capture.CanUseForTransaction || capture.Snapshot is null)
                {
                    capture.Snapshot?.Dispose();
                    return null;
                }

                return capture.Snapshot;
            }
            catch (ExternalException) when (attempt < 4)
            {
                logger.Log("CLIPBOARD_SNAPSHOT_RETRY", $"attempt={attempt + 1} reason=clipboard_busy");
                await delay(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.Log("ORIGINAL_CLIPBOARD_SNAPSHOT_FAILED", $"exception={exception.GetType().Name}");
                return null;
            }
        }

        logger.Log("ORIGINAL_CLIPBOARD_SNAPSHOT_FAILED", "reason=clipboard_busy");
        return null;
    }

    internal static void LogSnapshotCapture(ClipboardSnapshotCapture capture, DiagnosticLogger logger)
    {
        foreach (var format in capture.Formats)
        {
            logger.Log(
                format.Captured ? "CLIPBOARD_FORMAT_CAPTURED" : "CLIPBOARD_FORMAT_SKIPPED",
                $"format={format.Format} type={format.TypeName} reason={format.Reason}");
        }

        var metadata = $"status={capture.Status} sourceFormatCount={capture.SourceFormatCount} " +
            $"capturedFormatCount={capture.Snapshot?.FormatCount ?? 0}";
        switch (capture.Status)
        {
            case ClipboardSnapshotStatus.Empty:
            case ClipboardSnapshotStatus.Complete:
                logger.Log("ORIGINAL_CLIPBOARD_SNAPSHOT_OK", metadata);
                break;
            case ClipboardSnapshotStatus.Partial when capture.CanUseForTransaction:
                logger.Log("CLIPBOARD_SNAPSHOT_PARTIAL", metadata);
                break;
            case ClipboardSnapshotStatus.Partial:
            case ClipboardSnapshotStatus.Unusable:
                logger.Log("CLIPBOARD_SNAPSHOT_FATAL", metadata);
                break;
        }
    }

    internal async Task<ClipboardCopyResult> CopySelectedTextAsync(InputSimulator input, DiagnosticLogger logger, CancellationToken cancellationToken)
    {
        var sequenceBeforeCopy = NativeMethods.GetClipboardSequenceNumber();
        input.SendCtrlKey(Keys.C, SendInputOperation.Copy);
        logger.Log("COPY_SENT", $"sequenceBefore={sequenceBeforeCopy}");

        var result = await WaitForClipboardChangeAsync(
            sequenceBeforeCopy,
            NativeMethods.GetClipboardSequenceNumber,
            TryGetUnicodeText,
            token => Task.Delay(ClipboardPollIntervalMilliseconds, token),
            ClipboardTimeoutMilliseconds / ClipboardPollIntervalMilliseconds,
            cancellationToken);
        if (result.SequenceChanged)
        {
            logger.Log("CLIPBOARD_CHANGED", $"sequenceBefore={result.SequenceBefore} sequenceAfter={result.SequenceAfter} changed=yes");
            logger.Log("COPY_OBSERVED", $"sequenceBefore={result.SequenceBefore} sequenceAfter={result.SequenceAfter}");
            return result;
        }

        logger.Log("COPY_TIMEOUT", $"sequenceBefore={result.SequenceBefore} sequenceAfter={result.SequenceAfter} changed=no");
        return result;
    }

    internal static async Task<ClipboardCopyResult> WaitForClipboardChangeAsync(
        uint sequenceBefore,
        Func<uint> readSequence,
        Func<string?> readText,
        Func<CancellationToken, Task> delay,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var sequenceAfter = sequenceBefore;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            await delay(cancellationToken);
            sequenceAfter = readSequence();
            if (sequenceAfter != sequenceBefore)
            {
                return new ClipboardCopyResult(readText(), sequenceBefore, sequenceAfter, SequenceChanged: true);
            }
        }

        return new ClipboardCopyResult(Text: null, sequenceBefore, sequenceAfter, SequenceChanged: false);
    }

    public async Task<bool> SetUnicodeTextAsync(string text, CancellationToken cancellationToken) =>
        await TryClipboardActionAsync(() => Clipboard.SetDataObject(CreateTemporaryTextDataObject(text), copy: true), cancellationToken);

    internal async Task<bool> RestoreAsync(ClipboardSnapshot snapshot, DiagnosticLogger logger, CancellationToken cancellationToken)
    {
        logger.Log("ORIGINAL_CLIPBOARD_RESTORE_STARTED", $"empty={(snapshot.IsEmpty ? "yes" : "no")} formatCount={snapshot.FormatCount}");
        var restored = await RestoreSnapshotAsync(
            snapshot,
            Clipboard.Clear,
            dataObject => Clipboard.SetDataObject(dataObject, copy: true),
            cancellationToken);
        logger.Log(restored ? "ORIGINAL_CLIPBOARD_RESTORED" : "CLIPBOARD_RESTORE_FAILED");
        return restored;
    }

    internal static async Task<bool> RestoreSnapshotAsync(
        ClipboardSnapshot snapshot,
        Action clear,
        Action<DataObject> setDataObject,
        CancellationToken cancellationToken)
    {
        if (snapshot.IsEmpty)
        {
            return await TryClipboardActionAsync(clear, cancellationToken);
        }

        using var payload = snapshot.CreateRestorePayload();
        return await TryClipboardActionAsync(
            () => setDataObject(payload.DataObject),
            cancellationToken);
    }

    private static string? TryGetUnicodeText()
    {
        try
        {
            return Clipboard.ContainsText(TextDataFormat.UnicodeText)
                ? Clipboard.GetText(TextDataFormat.UnicodeText)
                : null;
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static async Task<bool> TryClipboardActionAsync(Action action, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                action();
                return true;
            }
            catch (ExternalException) when (attempt < 4)
            {
                await Task.Delay(15, cancellationToken);
            }
        }

        return false;
    }

    private static DataObject CreateTemporaryTextDataObject(string text)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, autoConvert: false, text);
        SetDwordFormat(data, "ExcludeClipboardContentFromMonitorProcessing", 1);
        SetDwordFormat(data, "CanIncludeInClipboardHistory", 0);
        SetDwordFormat(data, "CanUploadToCloudClipboard", 0);
        return data;
    }

    private static void SetDwordFormat(DataObject data, string format, uint value) =>
        data.SetData(format, autoConvert: false, new MemoryStream(BitConverter.GetBytes(value), writable: false));
}
