using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace KeyFlip;

internal enum UiControlKind
{
    Unknown,
    Edit,
    Document,
    List,
    ListItem,
    DataItem
}

internal enum VsCodeContext
{
    Editor,
    Terminal,
    Unknown
}

internal enum FocusedTargetContext
{
    Default,
    VsCodeEditor,
    VsCodeTerminal,
    VsCodeUnknown,
    ExplorerFileRename
}

internal readonly record struct UiElementDescriptor(
    UiControlKind ControlKind,
    string AutomationId,
    string ClassName,
    string FrameworkId);

internal static class FocusedContextClassifier
{
    private static readonly string[] TerminalMarkers = { "xterm", "terminal.integrated", "terminal-instance" };
    private static readonly string[] EditorMarkers = { "monaco-editor", "code-editor" };

    internal static VsCodeContext ClassifyVsCode(IReadOnlyList<UiElementDescriptor> elements)
    {
        if (elements.Any(element => HasMarker(element, TerminalMarkers))) return VsCodeContext.Terminal;
        if (elements.Any(element => HasMarker(element, EditorMarkers))) return VsCodeContext.Editor;
        return VsCodeContext.Unknown;
    }

    internal static bool IsExplorerFileRename(IReadOnlyList<UiElementDescriptor> elements) =>
        elements.Count > 1 &&
        elements[0].ControlKind == UiControlKind.Edit &&
        elements.Skip(1).Any(element => element.ControlKind is UiControlKind.ListItem or UiControlKind.DataItem);

    private static bool HasMarker(UiElementDescriptor element, IEnumerable<string> markers) =>
        markers.Any(marker =>
            element.AutomationId.Contains(marker, StringComparison.OrdinalIgnoreCase) ||
            element.ClassName.Contains(marker, StringComparison.OrdinalIgnoreCase));
}

internal sealed class FocusedContextDetector
{
    private const int MaximumElements = 12;

    internal FocusedTargetContext Detect(string executableName)
    {
        var isVsCode = string.Equals(executableName, "Code.exe", StringComparison.OrdinalIgnoreCase);
        var isExplorer = string.Equals(executableName, "explorer.exe", StringComparison.OrdinalIgnoreCase);
        if (!isVsCode && !isExplorer)
        {
            return FocusedTargetContext.Default;
        }

        try
        {
            var elements = CaptureFocusedElementAndAncestors();
            if (isExplorer)
            {
                return FocusedContextClassifier.IsExplorerFileRename(elements)
                    ? FocusedTargetContext.ExplorerFileRename
                    : FocusedTargetContext.Default;
            }

            return FocusedContextClassifier.ClassifyVsCode(elements) switch
            {
                VsCodeContext.Editor => FocusedTargetContext.VsCodeEditor,
                VsCodeContext.Terminal => FocusedTargetContext.VsCodeTerminal,
                _ => FocusedTargetContext.VsCodeUnknown
            };
        }
        catch (Exception exception) when (exception is
            ElementNotAvailableException or
            InvalidOperationException or
            UnauthorizedAccessException or
            COMException)
        {
            return isVsCode ? FocusedTargetContext.VsCodeUnknown : FocusedTargetContext.Default;
        }
    }

    private static IReadOnlyList<UiElementDescriptor> CaptureFocusedElementAndAncestors()
    {
        var elements = new List<UiElementDescriptor>(MaximumElements);
        var element = AutomationElement.FocusedElement;
        while (element is not null && elements.Count < MaximumElements)
        {
            elements.Add(CreateDescriptor(element));
            element = TreeWalker.ControlViewWalker.GetParent(element);
        }

        return elements;
    }

    private static UiElementDescriptor CreateDescriptor(AutomationElement element) => new(
        GetControlKind(element),
        GetStringProperty(element, AutomationElement.AutomationIdProperty),
        GetStringProperty(element, AutomationElement.ClassNameProperty),
        GetStringProperty(element, AutomationElement.FrameworkIdProperty));

    private static UiControlKind GetControlKind(AutomationElement element)
    {
        var value = element.GetCurrentPropertyValue(AutomationElement.ControlTypeProperty, ignoreDefaultValue: true);
        return value switch
        {
            ControlType controlType when controlType == ControlType.Edit => UiControlKind.Edit,
            ControlType controlType when controlType == ControlType.Document => UiControlKind.Document,
            ControlType controlType when controlType == ControlType.List => UiControlKind.List,
            ControlType controlType when controlType == ControlType.ListItem => UiControlKind.ListItem,
            ControlType controlType when controlType == ControlType.DataItem => UiControlKind.DataItem,
            _ => UiControlKind.Unknown
        };
    }

    private static string GetStringProperty(AutomationElement element, AutomationProperty property) =>
        element.GetCurrentPropertyValue(property, ignoreDefaultValue: true) as string ?? string.Empty;
}
