using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class FocusedContextClassifierTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        StrongXtermAutomationIdIsTerminal();
        StrongIntegratedTerminalClassIsTerminal();
        StrongMonacoClassIsEditor();
        GenericChromiumEditIsUnknown();
        GenericTerminalLabelIsUnknown();
        ExplorerEditUnderListItemIsRename();
        ExplorerSearchEditIsNotRename();
        ExplorerDocumentUnderListItemIsNotRename();
    }

    private void StrongXtermAutomationIdIsTerminal() => Equal(
        VsCodeContext.Terminal,
        Classify(new UiElementDescriptor(UiControlKind.Edit, "xterm-helper-textarea", string.Empty, "Chrome")));

    private void StrongIntegratedTerminalClassIsTerminal() => Equal(
        VsCodeContext.Terminal,
        Classify(new UiElementDescriptor(UiControlKind.Document, string.Empty, "terminal.integrated.instance", "Chrome")));

    private void StrongMonacoClassIsEditor() => Equal(
        VsCodeContext.Editor,
        Classify(new UiElementDescriptor(UiControlKind.Document, string.Empty, "monaco-editor", "Chrome")));

    private void GenericChromiumEditIsUnknown() => Equal(
        VsCodeContext.Unknown,
        Classify(new UiElementDescriptor(UiControlKind.Edit, string.Empty, "Chrome_RenderWidgetHostHWND", "Chrome")));

    private void GenericTerminalLabelIsUnknown() => Equal(
        VsCodeContext.Unknown,
        Classify(new UiElementDescriptor(UiControlKind.Document, "terminal", string.Empty, "Chrome")));

    private void ExplorerEditUnderListItemIsRename() => True(IsRename(
        new UiElementDescriptor(UiControlKind.Edit, string.Empty, "Edit", "Win32"),
        new UiElementDescriptor(UiControlKind.ListItem, string.Empty, string.Empty, "Win32")));

    private void ExplorerSearchEditIsNotRename() => False(IsRename(
        new UiElementDescriptor(UiControlKind.Edit, "SearchBox", "Edit", "Win32"),
        new UiElementDescriptor(UiControlKind.Unknown, "Toolbar", string.Empty, "Win32")));

    private void ExplorerDocumentUnderListItemIsNotRename() => False(IsRename(
        new UiElementDescriptor(UiControlKind.Document, string.Empty, string.Empty, "Chrome"),
        new UiElementDescriptor(UiControlKind.ListItem, string.Empty, string.Empty, "Win32")));

    private static VsCodeContext Classify(params UiElementDescriptor[] elements) =>
        FocusedContextClassifier.ClassifyVsCode(elements);

    private static bool IsRename(params UiElementDescriptor[] elements) =>
        FocusedContextClassifier.IsExplorerFileRename(elements);

    private void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected true, actual false.");
        Passed++;
    }

    private void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected false, actual true.");
        Passed++;
    }
}
