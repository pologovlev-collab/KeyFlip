using System.Text.Json;

namespace KeyFlip;

public sealed class SettingsService
{
    internal const int CurrentSchemaVersion = 2;
    private const HotkeyModifiers DefaultModifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift;
    private const int DefaultVirtualKey = (int)System.Windows.Forms.Keys.K;
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public SettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeyFlip",
        "settings.json")) { }

    internal SettingsService(string settingsPath) => _settingsPath = settingsPath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return CreateDefaults();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath));
            settings = Normalize(settings);
            if (MigrateLegacySettings(settings))
            {
                try { Save(settings); }
                catch (Exception) { }
            }

            return settings;
        }
        catch (Exception)
        {
            return CreateDefaults();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.SettingsSchemaVersion = CurrentSchemaVersion;
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Normalize(settings), SerializerOptions));
    }

    internal static bool MigrateLegacySettings(AppSettings settings)
    {
        if (settings.SettingsSchemaVersion >= CurrentSchemaVersion) return false;

        if (settings.HotkeyModifiers == (HotkeyModifiers.Control | HotkeyModifiers.Alt) &&
            settings.HotkeyVirtualKey == (int)System.Windows.Forms.Keys.K)
        {
            settings.HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift;
        }

        settings.SettingsSchemaVersion = CurrentSchemaVersion;
        return true;
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        settings ??= CreateDefaults();
        if (!IsSupportedModifiers(settings.HotkeyModifiers)) settings.HotkeyModifiers = DefaultModifiers;
        if (!IsSupportedVirtualKey(settings.HotkeyVirtualKey)) settings.HotkeyVirtualKey = DefaultVirtualKey;
        settings.ExcludedProcesses = (settings.ExcludedProcesses ?? new List<string>())
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return settings;
    }

    private static bool IsSupportedModifiers(HotkeyModifiers modifiers) => modifiers is
        HotkeyModifiers.Control or
        (HotkeyModifiers.Control | HotkeyModifiers.Alt) or
        (HotkeyModifiers.Control | HotkeyModifiers.Shift) or
        (HotkeyModifiers.Alt | HotkeyModifiers.Shift) or
        (HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift);

    private static bool IsSupportedVirtualKey(int virtualKey) =>
        virtualKey is >= (int)System.Windows.Forms.Keys.A and <= (int)System.Windows.Forms.Keys.Z or
        >= (int)System.Windows.Forms.Keys.D0 and <= (int)System.Windows.Forms.Keys.D9;

    private static AppSettings CreateDefaults() => new() { SettingsSchemaVersion = CurrentSchemaVersion };
}
