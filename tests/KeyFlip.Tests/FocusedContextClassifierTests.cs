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
        ExplorerSearchMarkerIsSearch();
        ExplorerAddressMarkerIsAddress();
        ExplorerGenericEditIsUnknown();
        FilenameShapeEnablesFallback();
        EmailAndSearchTextDoNotEnableFilenameFallback();
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

    private void ExplorerSearchMarkerIsSearch() => Equal(
        ExplorerContext.Search,
        ClassifyExplorer(new UiElementDescriptor(UiControlKind.Edit, "SearchTextBox", "Edit", "Win32")));

    private void ExplorerAddressMarkerIsAddress() => Equal(
        ExplorerContext.Address,
        ClassifyExplorer(
            new UiElementDescriptor(UiControlKind.Edit, "AddressEditBox", "Edit", "Win32"),
            new UiElementDescriptor(UiControlKind.Unknown, "", "Breadcrumb Parent", "Win32")));

    private void ExplorerGenericEditIsUnknown() => Equal(
        ExplorerContext.Unknown,
        ClassifyExplorer(new UiElementDescriptor(UiControlKind.Edit, string.Empty, "Edit", "Win32")));

    private void FilenameShapeEnablesFallback()
    {
        True(FileNameFallbackClassifier.IsLikelyFileName("ghbdtn.zip"));
        True(FileNameFallbackClassifier.IsLikelyFileName("ghbdtn.vbh.zip"));
        True(FileNameFallbackClassifier.IsLikelyFileName("photo.JPG"));
    }

    private void EmailAndSearchTextDoNotEnableFilenameFallback()
    {
        False(FileNameFallbackClassifier.IsLikelyFileName("test@example.com"));
        False(FileNameFallbackClassifier.IsLikelyFileName("https://example.com"));
        False(FileNameFallbackClassifier.IsLikelyFileName("kind:document"));
        False(FileNameFallbackClassifier.IsLikelyFileName("two words.txt"));
        False(FileNameFallbackClassifier.IsLikelyFileName(@"folder\file.txt"));
    }

    private static VsCodeContext Classify(params UiElementDescriptor[] elements) =>
        FocusedContextClassifier.ClassifyVsCode(elements);

    private static bool IsRename(params UiElementDescriptor[] elements) =>
        FocusedContextClassifier.IsExplorerFileRename(elements);

    private static ExplorerContext ClassifyExplorer(params UiElementDescriptor[] elements) =>
        FocusedContextClassifier.ClassifyExplorer(elements);

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
