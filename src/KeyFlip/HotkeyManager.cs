using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyFlip;

public sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 0x4B46;
    private const uint ModNoRepeat = 0x4000;
    private readonly IntPtr _windowHandle;
    private readonly Func<IntPtr, int, uint, uint, bool> _registerHotkey;
    private readonly Func<IntPtr, int, bool> _unregisterHotkey;
    private HotkeyConfiguration? _activeConfiguration;

    public HotkeyManager(IntPtr windowHandle) : this(
        windowHandle,
        NativeMethods.RegisterHotKey,
        NativeMethods.UnregisterHotKey) { }

    internal HotkeyManager(
        IntPtr windowHandle,
        Func<IntPtr, int, uint, uint, bool> registerHotkey,
        Func<IntPtr, int, bool> unregisterHotkey)
    {
        _windowHandle = windowHandle;
        _registerHotkey = registerHotkey;
        _unregisterHotkey = unregisterHotkey;
    }

    internal bool IsRegistered => _activeConfiguration is not null;
    internal HotkeyConfiguration? ActiveConfiguration => _activeConfiguration;

    public bool TryRegister(HotkeyConfiguration configuration, out string? error)
    {
        if (_activeConfiguration == configuration)
        {
            error = null;
            return true;
        }

        var previousConfiguration = _activeConfiguration;
        if (!TryDisable(out error)) return false;

        if (TryRegisterCore(configuration))
        {
            _activeConfiguration = configuration;
            error = null;
            return true;
        }

        var registrationError = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        if (previousConfiguration is { } previous)
        {
            if (TryRegisterCore(previous))
            {
                _activeConfiguration = previous;
                error = registrationError;
                return false;
            }

            _activeConfiguration = null;
            error = $"{registrationError}; previous hotkey could not be restored";
            return false;
        }

        error = registrationError;
        return false;
    }

    internal bool TryDisable(out string? error)
    {
        if (_activeConfiguration is null)
        {
            error = null;
            return true;
        }

        if (_unregisterHotkey(_windowHandle, HotkeyId))
        {
            _activeConfiguration = null;
            error = null;
            return true;
        }

        error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        return false;
    }

    public void Dispose() => TryDisable(out _);

    private bool TryRegisterCore(HotkeyConfiguration configuration) => _registerHotkey(
        _windowHandle,
        HotkeyId,
        (uint)configuration.Modifiers | ModNoRepeat,
        configuration.VirtualKey);
}
