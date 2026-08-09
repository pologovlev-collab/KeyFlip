using System.Text.Json;

namespace KeyFlip;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeyFlip", "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath));
            settings = Normalize(settings);
            if (MigrateLegacyDefaultHotkey(settings))
            {
                try { Save(settings); }
                catch (Exception) { }
            }

            return settings;
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(Normalize(settings), SerializerOptions));
    }

    internal static bool MigrateLegacyDefaultHotkey(AppSettings settings)
    {
        if (settings.HotkeyModifiers != (HotkeyModifiers.Control | HotkeyModifiers.Alt) ||
            settings.HotkeyVirtualKey != (int)System.Windows.Forms.Keys.K)
        {
            return false;
        }

        settings.HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Shift;
        return true;
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        settings ??= new AppSettings();
        settings.ExcludedProcesses = settings.ExcludedProcesses
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return settings;
    }
}
