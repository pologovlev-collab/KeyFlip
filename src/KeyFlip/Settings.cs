using System.Windows.Forms;

namespace KeyFlip;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004
}

public sealed class AppSettings
{
    public int SettingsSchemaVersion { get; set; }
    public bool Enabled { get; set; } = true;
    public HotkeyModifiers HotkeyModifiers { get; set; } = HotkeyModifiers.Control | HotkeyModifiers.Shift;
    public int HotkeyVirtualKey { get; set; } = (int)Keys.K;
    public bool StartWithWindows { get; set; } = true;
    public List<string> ExcludedProcesses { get; set; } = ForegroundProcessService.DefaultExcludedProcesses.ToList();

    public AppSettings Clone() => new()
    {
        SettingsSchemaVersion = SettingsSchemaVersion,
        Enabled = Enabled,
        HotkeyModifiers = HotkeyModifiers,
        HotkeyVirtualKey = HotkeyVirtualKey,
        StartWithWindows = StartWithWindows,
        ExcludedProcesses = ExcludedProcesses.ToList()
    };
}

public readonly record struct HotkeyConfiguration(HotkeyModifiers Modifiers, uint VirtualKey)
{
    public static HotkeyConfiguration From(AppSettings settings) => new(settings.HotkeyModifiers, (uint)settings.HotkeyVirtualKey);
}
