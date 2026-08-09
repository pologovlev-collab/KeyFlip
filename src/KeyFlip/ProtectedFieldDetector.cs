using System.Windows.Automation;

namespace KeyFlip;

internal sealed class ProtectedFieldDetector
{
    internal bool IsFocusedControlProtected()
    {
        try
        {
            var focusedElement = AutomationElement.FocusedElement;
            if (focusedElement is null) return false;

            var value = focusedElement.GetCurrentPropertyValue(AutomationElement.IsPasswordProperty, ignoreDefaultValue: true);
            return value is bool isPassword && isPassword;
        }
        catch (Exception exception) when (exception is ElementNotAvailableException or InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            return false;
        }
    }
}
