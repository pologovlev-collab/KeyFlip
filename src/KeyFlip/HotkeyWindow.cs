namespace KeyFlip;

internal sealed class HotkeyWindow : NativeWindow, IDisposable
{
    public event EventHandler? HotkeyPressed;

    public HotkeyWindow() => CreateHandle(new CreateParams());

    public void Dispose() => DestroyHandle();

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotkey)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref message);
    }
}

