using System.Runtime.InteropServices;

namespace KeyFlip;

internal enum WordReplacementResult
{
    Applied,
    Unchanged,
    Aborted
}

internal sealed class WordReplacementService
{
    private static readonly Guid WordApplicationClassId = new("000209FF-0000-0000-C000-000000000046");

    internal WordReplacementResult TryReplaceSelection(IntPtr sourceWindow, DiagnosticLogger logger)
    {
        object? application = null;
        object? activeWindow = null;
        object? selection = null;
        object? selectionRange = null;
        var editRanges = new List<(object Range, ConversionEdit Edit)>();
        try
        {
            var classId = WordApplicationClassId;
            GetActiveObject(ref classId, IntPtr.Zero, out application);
            dynamic word = application;
            activeWindow = word.ActiveWindow;
            var wordWindow = new IntPtr((int)((dynamic)activeWindow).Hwnd);
            if (wordWindow != sourceWindow || NativeMethods.GetForegroundWindow() != sourceWindow)
            {
                logger.Log("WORD_SAFE_ABORT", "reason=window_mismatch");
                return WordReplacementResult.Aborted;
            }

            selection = word.Selection;
            selectionRange = ((dynamic)selection).Range;
            dynamic sourceRange = selectionRange;
            var selectionStart = (int)sourceRange.Start;
            var selectionEnd = (int)sourceRange.End;
            var selectionText = sourceRange.Text as string;
            if (selectionText is null || selectionEnd - selectionStart != selectionText.Length)
            {
                logger.Log("WORD_SAFE_ABORT", "reason=selection_unavailable");
                return WordReplacementResult.Aborted;
            }

            var conversion = WordEditPlanner.Create(selectionText);
            if (!conversion.Changed)
            {
                logger.Log("WORD_SAFE_UNCHANGED");
                return WordReplacementResult.Unchanged;
            }

            foreach (var edit in conversion.Edits)
            {
                if (!IsSafeEdit(selectionText, edit))
                {
                    logger.Log("WORD_SAFE_ABORT", "reason=invalid_edit");
                    return WordReplacementResult.Aborted;
                }

                object range = sourceRange.Duplicate;
                ((dynamic)range).SetRange(selectionStart + edit.Start, selectionStart + edit.Start + edit.Length);
                editRanges.Add((range, edit));
            }

            if (NativeMethods.GetForegroundWindow() != sourceWindow)
            {
                logger.Log("WORD_SAFE_ABORT", "reason=foreground_changed");
                return WordReplacementResult.Aborted;
            }

            for (var index = editRanges.Count - 1; index >= 0; index--)
            {
                var (range, edit) = editRanges[index];
                ((dynamic)range).Text = edit.Replacement;
            }

            logger.Log("WORD_SAFE_APPLIED", $"edits={editRanges.Count}");
            return WordReplacementResult.Applied;
        }
        catch (Exception exception)
        {
            logger.Log("WORD_SAFE_ABORT", $"exception={exception.GetType().Name}");
            return WordReplacementResult.Aborted;
        }
        finally
        {
            foreach (var (range, _) in editRanges) ReleaseComObject(range);
            ReleaseComObject(selectionRange);
            ReleaseComObject(selection);
            ReleaseComObject(activeWindow);
            ReleaseComObject(application);
        }
    }

    private static bool IsSafeEdit(string text, ConversionEdit edit) =>
        edit.Start >= 0 && edit.Length > 0 && edit.Start + edit.Length <= text.Length &&
        !text.AsSpan(edit.Start, edit.Length).ContainsAny('\r', '\n', '\a');

    private static void ReleaseComObject(object? instance)
    {
        try
        {
            if (instance is not null && Marshal.IsComObject(instance)) Marshal.ReleaseComObject(instance);
        }
        catch (Exception)
        {
            // Releasing an automation proxy must not affect conversion or Word.
        }
    }

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern void GetActiveObject(
        ref Guid classId,
        IntPtr reserved,
        [MarshalAs(UnmanagedType.IUnknown)] out object activeObject);
}
