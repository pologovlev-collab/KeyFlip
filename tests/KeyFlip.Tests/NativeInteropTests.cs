using System.Runtime.InteropServices;

namespace KeyFlip.Tests;

internal sealed class NativeInteropTests
{
    public int Passed { get; private set; }

    public void Run()
    {
        InputHasNativeCompatibleSize();
        DefaultHotkeyIsCtrlAltK();
    }

    private void InputHasNativeCompatibleSize()
    {
        var expectedSize = IntPtr.Size == 8 ? 40 : 28;
        Equal(expectedSize, Marshal.SizeOf<NativeMethods.Input>());
    }

    private void DefaultHotkeyIsCtrlAltK()
    {
        var settings = new AppSettings();
        Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, settings.HotkeyModifiers);
        Equal((int)System.Windows.Forms.Keys.K, settings.HotkeyVirtualKey);
    }

    private void Equal<T>(T expected, T actual) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected '{expected}', actual '{actual}'.");
        }

        Passed++;
    }
}
