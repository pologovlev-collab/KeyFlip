using System.Text.Json;

namespace KeyFlip;

public sealed class SettingsService
{
    internal const int CurrentSchemaVersion = 2;
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
        settings.ExcludedProcesses = (settings.ExcludedProcesses ?? new List<string>())
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return settings;
    }

    private static AppSettings CreateDefaults() => new() { SettingsSchemaVersion = CurrentSchemaVersion };
}
