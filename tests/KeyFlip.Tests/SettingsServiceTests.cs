using System.Windows.Forms;

namespace KeyFlip.Tests;

internal sealed class SettingsServiceTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        MigratesExactHistoricalDefault();
        KeepsCustomCtrlAltHotkey();
        KeepsCustomCtrlShiftHotkey();
        MigrationRunsOnlyOnce();
        ManualCtrlAltChoiceSurvivesSchemaTwo();
        ManualCtrlAltChoiceSurvivesReload();
        InvalidHotkeyValuesFallBackToDefaults();
        CorruptedJsonFallsBackToDefaults();
    }

    private void MigratesExactHistoricalDefault()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.K);

        True(SettingsService.MigrateLegacySettings(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyModifiers);
        Equal((int)Keys.K, settings.HotkeyVirtualKey);
        Equal(SettingsService.CurrentSchemaVersion, settings.SettingsSchemaVersion);
    }

    private void KeepsCustomCtrlAltHotkey()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.L);

        True(SettingsService.MigrateLegacySettings(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.HotkeyModifiers);
        Equal((int)Keys.L, settings.HotkeyVirtualKey);
    }

    private void KeepsCustomCtrlShiftHotkey()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.P);

        True(SettingsService.MigrateLegacySettings(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyModifiers);
        Equal((int)Keys.P, settings.HotkeyVirtualKey);
    }

    private void MigrationRunsOnlyOnce()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.K);

        True(SettingsService.MigrateLegacySettings(settings));
        False(SettingsService.MigrateLegacySettings(settings));
    }

    private void ManualCtrlAltChoiceSurvivesSchemaTwo()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.K);
        settings.SettingsSchemaVersion = SettingsService.CurrentSchemaVersion;

        False(SettingsService.MigrateLegacySettings(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.HotkeyModifiers);
        Equal((int)Keys.K, settings.HotkeyVirtualKey);
    }

    private void ManualCtrlAltChoiceSurvivesReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KeyFlip.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "{\"HotkeyModifiers\":3,\"HotkeyVirtualKey\":75}");
            var service = new SettingsService(path);
            var migrated = service.Load();
            Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, migrated.HotkeyModifiers);

            migrated.HotkeyModifiers = HotkeyModifiers.Control | HotkeyModifiers.Alt;
            service.Save(migrated);
            var reloaded = service.Load();
            Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, reloaded.HotkeyModifiers);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private void InvalidHotkeyValuesFallBackToDefaults()
    {
        var settings = LoadFromJson("{\"SettingsSchemaVersion\":2,\"HotkeyModifiers\":4294967295,\"HotkeyVirtualKey\":999}");

        Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyModifiers);
        Equal((int)Keys.K, settings.HotkeyVirtualKey);
    }

    private void CorruptedJsonFallsBackToDefaults()
    {
        var settings = LoadFromJson("{broken json");

        Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyModifiers);
        Equal((int)Keys.K, settings.HotkeyVirtualKey);
    }

    private static AppSettings LoadFromJson(string json)
    {
        var directory = Path.Combine(Path.GetTempPath(), "KeyFlip.Tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "settings.json");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, json);
            return new SettingsService(path).Load();
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    private static AppSettings Create(HotkeyModifiers modifiers, Keys key) => new()
    {
        HotkeyModifiers = modifiers,
        HotkeyVirtualKey = (int)key
    };

    private void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }

    private void True(bool value)
    {
        if (!value) throw new InvalidOperationException("Expected true, actual false.");
        Passed++;
    }

    private void False(bool value)
    {
        if (value) throw new InvalidOperationException("Expected false, actual true.");
        Passed++;
    }
}
