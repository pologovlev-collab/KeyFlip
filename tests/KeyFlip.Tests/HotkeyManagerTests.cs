using KeyFlip;

namespace KeyFlip.Tests;

internal sealed class HotkeyManagerTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        DisableUnregistersActiveHotkey();
        FailedReplacementRestoresPreviousHotkey();
        FailedRollbackLeavesNoClaimedHotkey();
    }

    private void DisableUnregistersActiveHotkey()
    {
        var unregisterCalls = 0;
        using var manager = Create(
            registerResults: new[] { true },
            unregister: () => { unregisterCalls++; return true; });

        True(manager.TryRegister(ControlShiftK(), out _));
        True(manager.IsRegistered);
        True(manager.TryDisable(out _));
        False(manager.IsRegistered);
        Equal(1, unregisterCalls);
    }

    private void FailedReplacementRestoresPreviousHotkey()
    {
        using var manager = Create(new[] { true, false, true }, () => true);

        True(manager.TryRegister(ControlShiftK(), out _));
        False(manager.TryRegister(ControlAltK(), out _));
        True(manager.IsRegistered);
        Equal(ControlShiftK(), manager.ActiveConfiguration!.Value);
    }

    private void FailedRollbackLeavesNoClaimedHotkey()
    {
        using var manager = Create(new[] { true, false, false }, () => true);

        True(manager.TryRegister(ControlShiftK(), out _));
        False(manager.TryRegister(ControlAltK(), out var error));
        False(manager.IsRegistered);
        True(error?.Contains("previous", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static HotkeyManager Create(IEnumerable<bool> registerResults, Func<bool> unregister)
    {
        var results = new Queue<bool>(registerResults);
        return new HotkeyManager(
            IntPtr.Zero,
            (_, _, _, _) => results.Dequeue(),
            (_, _) => unregister());
    }

    private static HotkeyConfiguration ControlShiftK() =>
        new(HotkeyModifiers.Control | HotkeyModifiers.Shift, (uint)System.Windows.Forms.Keys.K);

    private static HotkeyConfiguration ControlAltK() =>
        new(HotkeyModifiers.Control | HotkeyModifiers.Alt, (uint)System.Windows.Forms.Keys.K);

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
