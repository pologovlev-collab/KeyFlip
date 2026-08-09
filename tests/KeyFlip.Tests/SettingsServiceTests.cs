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
    }

    private void MigratesExactHistoricalDefault()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.K);

        True(SettingsService.MigrateLegacyDefaultHotkey(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyModifiers);
        Equal((int)Keys.K, settings.HotkeyVirtualKey);
    }

    private void KeepsCustomCtrlAltHotkey()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Alt, Keys.L);

        False(SettingsService.MigrateLegacyDefaultHotkey(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.HotkeyModifiers);
        Equal((int)Keys.L, settings.HotkeyVirtualKey);
    }

    private void KeepsCustomCtrlShiftHotkey()
    {
        var settings = Create(HotkeyModifiers.Control | HotkeyModifiers.Shift, Keys.P);

        False(SettingsService.MigrateLegacyDefaultHotkey(settings));
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Shift, settings.HotkeyModifiers);
        Equal((int)Keys.P, settings.HotkeyVirtualKey);
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
