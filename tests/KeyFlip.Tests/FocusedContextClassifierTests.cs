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

    private static VsCodeContext Classify(params UiElementDescriptor[] elements) =>
        FocusedContextClassifier.ClassifyVsCode(elements);

    private void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }
}
