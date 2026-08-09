using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyFlip;

public sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 0x4B46;
    private const uint ModNoRepeat = 0x4000;
    private readonly IntPtr _windowHandle;
    private HotkeyConfiguration? _activeConfiguration;

    public HotkeyManager(IntPtr windowHandle) => _windowHandle = windowHandle;

    public bool TryRegister(HotkeyConfiguration configuration, out string? error)
    {
        var previousConfiguration = _activeConfiguration;
        Unregister();

        if (TryRegisterCore(configuration))
        {
            _activeConfiguration = configuration;
            error = null;
            return true;
        }

        if (previousConfiguration is { } previous)
        {
            TryRegisterCore(previous);
            _activeConfiguration = previous;
        }

        error = new Win32Exception(Marshal.GetLastWin32Error()).Message;
        return false;
    }

    public void Dispose() => Unregister();

    private bool TryRegisterCore(HotkeyConfiguration configuration) => NativeMethods.RegisterHotKey(
        _windowHandle,
        HotkeyId,
        (uint)configuration.Modifiers | ModNoRepeat,
        configuration.VirtualKey);

    private void Unregister()
    {
        if (_activeConfiguration is null) return;
        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyId);
        _activeConfiguration = null;
    }
}
