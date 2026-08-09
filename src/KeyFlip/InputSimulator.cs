using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

internal enum SendInputOperation
{
    Copy,
    Paste
}

internal sealed class SendInputException : Exception
{
    public SendInputException(SendInputOperation operation, uint expected, uint sent, int win32Error, int inputSize)
        : base($"SendInput {operation} failed. Sent={sent}/{expected}, Win32Error={win32Error}, InputSize={inputSize}")
    {
        Operation = operation;
        Expected = expected;
        Sent = sent;
        Win32Error = win32Error;
        InputSize = inputSize;
    }

    public SendInputOperation Operation { get; }
    public uint Expected { get; }
    public uint Sent { get; }
    public int Win32Error { get; }
    public int InputSize { get; }

    public string ToDiagnosticMetadata() => $"sent={Sent}/{Expected} win32Error={Win32Error} inputSize={InputSize}";
}

public sealed class InputSimulator
{
    public async Task<bool> WaitForHotkeyModifiersToReleaseAsync(CancellationToken cancellationToken)
    {
        var deadline = Environment.TickCount64 + 1000;
        while (AnyModifierPressed())
        {
            if (Environment.TickCount64 >= deadline) return false;
            await Task.Delay(10, cancellationToken);
        }

        await Task.Delay(25, cancellationToken);
        return true;
    }

    internal void SendCtrlKey(Keys key, SendInputOperation operation)
    {
        var virtualKey = checked((ushort)key);
        var inputs = new[]
        {
            CreateKeyInput(NativeMethods.VirtualKeyControl, 0),
            CreateKeyInput(virtualKey, 0),
            CreateKeyInput(virtualKey, NativeMethods.KeyEventKeyUp),
            CreateKeyInput(NativeMethods.VirtualKeyControl, NativeMethods.KeyEventKeyUp)
        };

        var inputSize = Marshal.SizeOf<NativeMethods.Input>();
        var expected = (uint)inputs.Length;
        var sent = NativeMethods.SendInput(expected, inputs, inputSize);
        if (sent != inputs.Length)
        {
            throw new SendInputException(operation, expected, sent, Marshal.GetLastWin32Error(), inputSize);
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
