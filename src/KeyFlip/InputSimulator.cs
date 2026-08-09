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
    public SendInputException(SendInputOperation operation, uint expected, uint sent, int win32Error, int inputSize, bool isCleanup = false)
        : base($"SendInput {operation} failed. Sent={sent}/{expected}, Win32Error={win32Error}, InputSize={inputSize}")
    {
        Operation = operation;
        Expected = expected;
        Sent = sent;
        Win32Error = win32Error;
        InputSize = inputSize;
        IsCleanup = isCleanup;
    }

    public SendInputOperation Operation { get; }
    public uint Expected { get; }
    public uint Sent { get; }
    public int Win32Error { get; }
    public int InputSize { get; }
    public bool IsCleanup { get; }

    public string ToDiagnosticMetadata() => $"sent={Sent}/{Expected} win32Error={Win32Error} inputSize={InputSize}";
}

internal sealed class PhysicalModifierPressedException(SendInputOperation operation) : Exception
{
    internal SendInputOperation Operation { get; } = operation;
}

internal sealed class ModifierReleaseTracker(int requiredUpPolls)
{
    private int _consecutiveUpPolls;

    internal bool Observe(bool anyModifierPressed)
    {
        _consecutiveUpPolls = anyModifierPressed ? 0 : _consecutiveUpPolls + 1;
        return _consecutiveUpPolls >= requiredUpPolls;
    }
}

public sealed class InputSimulator
{
    private static readonly int[] PhysicalModifierKeys =
    {
        NativeMethods.VirtualKeyLeftControl, NativeMethods.VirtualKeyRightControl,
        NativeMethods.VirtualKeyLeftShift, NativeMethods.VirtualKeyRightShift,
        NativeMethods.VirtualKeyLeftAlt, NativeMethods.VirtualKeyRightAlt,
        NativeMethods.VirtualKeyLeftWindows, NativeMethods.VirtualKeyRightWindows
    };

    private readonly Func<NativeMethods.Input[], uint> _sendInputs;
    private readonly Func<int, short> _getKeyState;

    public InputSimulator()
        : this(
            inputs => NativeMethods.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeMethods.Input>()),
            NativeMethods.GetAsyncKeyState)
    {
    }

    internal InputSimulator(Func<NativeMethods.Input[], uint> sendInputs, Func<int, short> getKeyState)
    {
        _sendInputs = sendInputs;
        _getKeyState = getKeyState;
    }

    public async Task<bool> WaitForHotkeyModifiersToReleaseAsync(CancellationToken cancellationToken)
    {
        var deadline = Environment.TickCount64 + 1000;
        var tracker = new ModifierReleaseTracker(requiredUpPolls: 3);
        while (!tracker.Observe(AnyModifierPressed()))
        {
            if (Environment.TickCount64 >= deadline) return false;
            await Task.Delay(15, cancellationToken);
        }

        await Task.Delay(45, cancellationToken);
        return !AnyModifierPressed();
    }

    internal void SendCtrlKey(Keys key, SendInputOperation operation)
    {
        if (AnyModifierPressed()) throw new PhysicalModifierPressedException(operation);

        var virtualKey = checked((ushort)key);
        var controlDownSent = false;
        try
        {
            SendBatch(new[] { CreateKeyInput(NativeMethods.VirtualKeyControl, 0) }, operation, isCleanup: false);
            controlDownSent = true;
            SendBatch(
                new[]
                {
                    CreateKeyInput(virtualKey, 0),
                    CreateKeyInput(virtualKey, NativeMethods.KeyEventKeyUp)
                },
                operation,
                isCleanup: false);
        }
        finally
        {
            if (controlDownSent)
            {
                SendBatch(new[] { CreateKeyInput(NativeMethods.VirtualKeyControl, NativeMethods.KeyEventKeyUp) }, operation, isCleanup: true);
            }
        }
    }

    internal bool ArePhysicalModifiersReleased() => !AnyModifierPressed();

    private void SendBatch(NativeMethods.Input[] inputs, SendInputOperation operation, bool isCleanup)
    {
        var inputSize = Marshal.SizeOf<NativeMethods.Input>();
        var expected = (uint)inputs.Length;
        var sent = _sendInputs(inputs);
        if (sent != inputs.Length)
        {
            throw new SendInputException(operation, expected, sent, Marshal.GetLastWin32Error(), inputSize, isCleanup);
        }
    }

    private bool AnyModifierPressed() => PhysicalModifierKeys.Any(IsPressed);

    private bool IsPressed(int virtualKey) => (_getKeyState(virtualKey) & 0x8000) != 0;

    private static NativeMethods.Input CreateKeyInput(ushort virtualKey, uint flags) => new()
    {
        Type = NativeMethods.InputKeyboard,
        Union = new NativeMethods.InputUnion
        {
            Keyboard = new NativeMethods.KeyboardInput { VirtualKey = virtualKey, Flags = flags }
        }
    };
}
