using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

public sealed class InputSimulator
{
    public async Task WaitForHotkeyModifiersToReleaseAsync(CancellationToken cancellationToken)
    {
        var deadline = Environment.TickCount64 + 250;
        while (Environment.TickCount64 < deadline && AnyModifierPressed())
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    public void SendCtrlKey(Keys key)
    {
        var virtualKey = checked((ushort)key);
        var inputs = new[]
        {
            CreateKeyInput(NativeMethods.VirtualKeyControl, 0),
            CreateKeyInput(virtualKey, 0),
            CreateKeyInput(virtualKey, NativeMethods.KeyEventKeyUp),
            CreateKeyInput(NativeMethods.VirtualKeyControl, NativeMethods.KeyEventKeyUp)
        };

        var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>());
        if (sent != inputs.Length)
        {
            throw new InvalidOperationException("Windows did not accept the complete keyboard input sequence.");
        }
    }

    private static bool AnyModifierPressed() =>
        IsPressed(NativeMethods.VirtualKeyControl) || IsPressed(NativeMethods.VirtualKeyMenu) || IsPressed(NativeMethods.VirtualKeyShift);

    private static bool IsPressed(int virtualKey) => (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;

    private static NativeMethods.Input CreateKeyInput(ushort virtualKey, uint flags) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Union = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput { VirtualKey = virtualKey, Flags = flags }
        }
    };
}

