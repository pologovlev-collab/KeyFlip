using Microsoft.Win32;

namespace KeyFlip;

public sealed class StartupManager
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string ValueName = "KeyFlip";

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to open the current-user startup registry key.");

        if (enabled)
        {
            key.SetValue(ValueName, BuildStartupCommand(Environment.ProcessPath));
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }

    internal static string BuildStartupCommand(string? processPath)
    {
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("Unable to determine the current executable path.");
        }

        return $"\"{processPath}\"";
    }
}
