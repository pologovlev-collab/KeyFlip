using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

public sealed record ClipboardCopyResult(string? Text, uint SequenceBefore, uint SequenceAfter, bool SequenceChanged);

public sealed class ClipboardService
{
    private const int ClipboardTimeoutMilliseconds = 900;

    internal async Task<ClipboardSnapshot?> CaptureSnapshotAsync(DiagnosticLogger logger, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var source = Clipboard.GetDataObject();
                if (!ClipboardSnapshot.TryCreate(source, out var snapshot))
                {
                    logger.Log("ORIGINAL_CLIPBOARD_SNAPSHOT_FAILED", "reason=unsupported_or_delayed_format");
                    return null;
                }

                logger.Log("ORIGINAL_CLIPBOARD_SNAPSHOT_OK", $"empty={(snapshot.IsEmpty ? "yes" : "no")} formatCount={snapshot.FormatCount}");
                return snapshot;
            }
            catch (ExternalException) when (attempt < 4)
            {
                await Task.Delay(20, cancellationToken);
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

    internal async Task<ClipboardCopyResult> CopySelectedTextAsync(InputSimulator input, DiagnosticLogger logger, CancellationToken cancellationToken)
    {
        var sequenceBeforeCopy = NativeMethods.GetClipboardSequenceNumber();
        input.SendCtrlKey(Keys.C, SendInputOperation.Copy);
        logger.Log("COPY_SENT", $"sequenceBefore={sequenceBeforeCopy}");

        var deadline = Environment.TickCount64 + ClipboardTimeoutMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            await Task.Delay(15, cancellationToken);
            var sequenceAfterCopy = NativeMethods.GetClipboardSequenceNumber();
            if (sequenceAfterCopy == sequenceBeforeCopy) continue;

            logger.Log("CLIPBOARD_CHANGED", $"sequenceBefore={sequenceBeforeCopy} sequenceAfter={sequenceAfterCopy} changed=yes");
            logger.Log("COPY_OBSERVED", $"sequenceBefore={sequenceBeforeCopy} sequenceAfter={sequenceAfterCopy}");
            return new ClipboardCopyResult(TryGetUnicodeText(), sequenceBeforeCopy, sequenceAfterCopy, true);
        }

        var timeoutSequenceAfter = NativeMethods.GetClipboardSequenceNumber();
        logger.Log("COPY_TIMEOUT", $"sequenceBefore={sequenceBeforeCopy} sequenceAfter={timeoutSequenceAfter} changed=no");
        return new ClipboardCopyResult(null, sequenceBeforeCopy, timeoutSequenceAfter, false);
    }

    public async Task<bool> SetUnicodeTextAsync(string text, CancellationToken cancellationToken) =>
        await TryClipboardActionAsync(() => Clipboard.SetDataObject(CreateTemporaryTextDataObject(text), copy: true), cancellationToken);

    internal async Task<bool> RestoreAsync(ClipboardSnapshot snapshot, DiagnosticLogger logger, CancellationToken cancellationToken)
    {
        logger.Log("ORIGINAL_CLIPBOARD_RESTORE_STARTED", $"empty={(snapshot.IsEmpty ? "yes" : "no")} formatCount={snapshot.FormatCount}");
        var restored = await TryClipboardActionAsync(
            () =>
            {
                if (snapshot.IsEmpty) Clipboard.Clear();
                else Clipboard.SetDataObject(snapshot.CreateDataObject(), copy: true);
            },
            cancellationToken);
        logger.Log(restored ? "ORIGINAL_CLIPBOARD_RESTORED" : "CLIPBOARD_RESTORE_FAILED");
        return restored;
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
