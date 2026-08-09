using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

public sealed record ClipboardCopyResult(IDataObject? OriginalClipboard, string? Text);

public sealed class ClipboardService
{
    private const int ClipboardTimeoutMilliseconds = 350;

    public async Task<ClipboardCopyResult> CopySelectedTextAsync(InputSimulator input, CancellationToken cancellationToken)
    {
        var originalClipboard = TryGetDataObject();
        var sequenceBeforeCopy = NativeMethods.GetClipboardSequenceNumber();
        input.SendCtrlKey(Keys.C);

        var deadline = Environment.TickCount64 + ClipboardTimeoutMilliseconds;
        while (Environment.TickCount64 < deadline)
        {
            await Task.Delay(15, cancellationToken);
            if (NativeMethods.GetClipboardSequenceNumber() == sequenceBeforeCopy) continue;

            return new ClipboardCopyResult(originalClipboard, TryGetUnicodeText());
        }

        return new ClipboardCopyResult(originalClipboard, null);
    }

    public async Task<bool> SetUnicodeTextAsync(string text, CancellationToken cancellationToken) =>
        await TryClipboardActionAsync(() => Clipboard.SetDataObject(new DataObject(DataFormats.UnicodeText, text), copy: true), cancellationToken);

    public async Task RestoreAsync(IDataObject? originalClipboard, CancellationToken cancellationToken) =>
        await TryClipboardActionAsync(
            () =>
            {
                if (originalClipboard is null) Clipboard.Clear();
                else Clipboard.SetDataObject(originalClipboard, copy: true);
            },
            cancellationToken);

    private static IDataObject? TryGetDataObject()
    {
        try { return Clipboard.GetDataObject(); }
        catch (ExternalException) { return null; }
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

