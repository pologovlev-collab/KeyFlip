using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

public sealed record ClipboardCopyResult(
    bool HasOriginalClipboardSnapshot,
    IDataObject? OriginalClipboard,
    string? Text,
    uint SequenceBefore,
    uint SequenceAfter,
    bool SequenceChanged);

public sealed class ClipboardService
{
    private const int ClipboardTimeoutMilliseconds = 900;

    internal async Task<ClipboardCopyResult> CopySelectedTextAsync(InputSimulator input, DiagnosticLogger logger, CancellationToken cancellationToken)
    {
        var (hasOriginalClipboardSnapshot, originalClipboard) = TryGetDataObject();
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
            return new ClipboardCopyResult(hasOriginalClipboardSnapshot, originalClipboard, TryGetUnicodeText(), sequenceBeforeCopy, sequenceAfterCopy, true);
        }

        var timeoutSequenceAfter = NativeMethods.GetClipboardSequenceNumber();
        logger.Log("COPY_TIMEOUT", $"sequenceBefore={sequenceBeforeCopy} sequenceAfter={timeoutSequenceAfter} changed=no");
        return new ClipboardCopyResult(hasOriginalClipboardSnapshot, originalClipboard, null, sequenceBeforeCopy, timeoutSequenceAfter, false);
    }

    public async Task<bool> SetUnicodeTextAsync(string text, CancellationToken cancellationToken) =>
        await TryClipboardActionAsync(() => Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, text), copy: true), cancellationToken);

    public async Task<bool> RestoreAsync(bool hasOriginalClipboardSnapshot, IDataObject? originalClipboard, CancellationToken cancellationToken)
    {
        if (!hasOriginalClipboardSnapshot) return false;
        return await TryClipboardActionAsync(
            () =>
            {
                if (originalClipboard is null) Clipboard.Clear();
                else Clipboard.SetDataObject(originalClipboard, copy: true);
            },
            cancellationToken);
    }

    private static (bool Success, IDataObject? Data) TryGetDataObject()
    {
        try { return (true, Clipboard.GetDataObject()); }
        catch (ExternalException) { return (false, null); }
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
}
