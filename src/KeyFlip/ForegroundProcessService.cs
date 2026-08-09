using System.Diagnostics;

namespace KeyFlip;

public sealed class ForegroundProcessService
{
    public static readonly string[] DefaultExcludedProcesses =
    {
        "WindowsTerminal.exe", "cmd.exe", "powershell.exe", "pwsh.exe", "conhost.exe",
        "OpenConsole.exe", "wt.exe", "wsl.exe", "bash.exe", "mintty.exe"
    };

    public bool IsExcludedForegroundProcess(IEnumerable<string> excludedProcesses)
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return false;

        NativeMethods.GetWindowThreadProcessId(foregroundWindow, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return IsExcludedProcessName($"{process.ProcessName}.exe", excludedProcesses);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool IsExcludedProcessName(string processName, IEnumerable<string> excludedProcesses) =>
        excludedProcesses.Any(name => string.Equals(
            Path.GetFileName(name.Trim()), Path.GetFileName(processName.Trim()), StringComparison.OrdinalIgnoreCase));
}

