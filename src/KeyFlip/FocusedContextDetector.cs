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

internal enum ExplorerContext
{
    Rename,
    Search,
    Address,
    Unknown
}

internal enum FocusedTargetContext
{
    Default,
    VsCodeEditor,
    VsCodeTerminal,
    VsCodeUnknown,
    ExplorerFileRename,
    ExplorerSearch,
    ExplorerAddress,
    ExplorerUnknown
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
    private static readonly string[] ExplorerSearchMarkers = { "search", "universalsearch" };
    private static readonly string[] ExplorerAddressMarkers = { "address", "breadcrumb", "location", "travelband" };

    internal static VsCodeContext ClassifyVsCode(IReadOnlyList<UiElementDescriptor> elements)
    {
        if (elements.Any(element => HasMarker(element, TerminalMarkers))) return VsCodeContext.Terminal;
        if (elements.Any(element => HasMarker(element, EditorMarkers))) return VsCodeContext.Editor;
        return VsCodeContext.Unknown;
    }

    internal static ExplorerContext ClassifyExplorer(IReadOnlyList<UiElementDescriptor> elements)
    {
        if (elements.Count == 0 || elements[0].ControlKind != UiControlKind.Edit) return ExplorerContext.Unknown;
        if (elements.Any(element => HasMarker(element, ExplorerSearchMarkers))) return ExplorerContext.Search;
        if (elements.Any(element => HasMarker(element, ExplorerAddressMarkers))) return ExplorerContext.Address;
        if (elements.Skip(1).Any(element => element.ControlKind is UiControlKind.ListItem or UiControlKind.DataItem))
        {
            return ExplorerContext.Rename;
        }

        return ExplorerContext.Unknown;
    }

    internal static bool IsExplorerFileRename(IReadOnlyList<UiElementDescriptor> elements) =>
        ClassifyExplorer(elements) == ExplorerContext.Rename;

    private static bool HasMarker(UiElementDescriptor element, IEnumerable<string> markers) =>
        markers.Any(marker =>
            element.AutomationId.Contains(marker, StringComparison.OrdinalIgnoreCase) ||
            element.ClassName.Contains(marker, StringComparison.OrdinalIgnoreCase));
}

internal static class FileNameFallbackClassifier
{
    private const int MaximumExtensionLength = 10;
    private const string RejectedCharacters = "@:/\\?*\"<>|";

    internal static bool IsLikelyFileName(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
        {
            return false;
        }

        if (text.IndexOfAny(RejectedCharacters.ToCharArray()) >= 0) return false;
        var lastDot = text.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == text.Length - 1) return false;

        var extension = text[(lastDot + 1)..];
        if (extension.Length > MaximumExtensionLength || !extension.All(char.IsLetterOrDigit)) return false;
        return text[..lastDot].Split('.').All(static segment => segment.Length > 0);
    }
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
                return FocusedContextClassifier.ClassifyExplorer(elements) switch
                {
                    ExplorerContext.Rename => FocusedTargetContext.ExplorerFileRename,
                    ExplorerContext.Search => FocusedTargetContext.ExplorerSearch,
                    ExplorerContext.Address => FocusedTargetContext.ExplorerAddress,
                    _ => FocusedTargetContext.ExplorerUnknown
                };
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
            return isVsCode ? FocusedTargetContext.VsCodeUnknown : FocusedTargetContext.ExplorerUnknown;
        }
    }

    private static IReadOnlyList<UiElementDescriptor> CaptureFocusedElementAndAncestors()
    {
        var elements = new List<UiElementDescriptor>(MaximumElements);
        var focused = AutomationElement.FocusedElement;
        if (focused is null) return elements;

        elements.Add(CreateDescriptor(focused));
        AppendAncestors(elements, focused, TreeWalker.ControlViewWalker);
        AppendAncestors(elements, focused, TreeWalker.RawViewWalker);
        return elements;
    }

    private static void AppendAncestors(
        ICollection<UiElementDescriptor> elements,
        AutomationElement focused,
        TreeWalker walker)
    {
        var element = walker.GetParent(focused);
        while (element is not null && elements.Count < MaximumElements)
        {
            elements.Add(CreateDescriptor(element));
            element = walker.GetParent(element);
        }
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
