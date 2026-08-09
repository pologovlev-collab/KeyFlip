using System.Drawing;
using System.Windows.Forms;

namespace KeyFlip;

public sealed class KeyFlipContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private readonly StartupManager _startupManager = new();
    private readonly ForegroundProcessService _foregroundProcessService = new();
    private readonly InputSimulator _inputSimulator = new();
    private readonly ClipboardService _clipboardService = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly HotkeyManager _hotkeyManager;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _enabledItem;
    private readonly ToolStripMenuItem _autostartItem;
    private AppSettings _settings;

    public KeyFlipContext()
    {
        _settings = _settingsService.Load();
        _hotkeyManager = new HotkeyManager(_hotkeyWindow.Handle);
        _hotkeyWindow.HotkeyPressed += (_, _) => _ = ConvertSelectionAsync();

        _enabledItem = new ToolStripMenuItem("Включено", null, (_, _) => ToggleEnabled()) { Checked = _settings.Enabled };
        _autostartItem = new ToolStripMenuItem("Автозапуск", null, (_, _) => ToggleAutostart()) { Checked = _settings.StartWithWindows };
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem("KeyFlip") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_enabledItem);
        menu.Items.Add(new ToolStripMenuItem("Настройки", null, (_, _) => ShowSettings()));
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Выход", null, (_, _) => ExitThread()));
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "KeyFlip",
            ContextMenuStrip = menu,
            Visible = true
        };

        if (!_hotkeyManager.TryRegister(HotkeyConfiguration.From(_settings), out var error))
        {
            _trayIcon.ShowBalloonTip(3000, "KeyFlip", $"Не удалось зарегистрировать горячую клавишу: {error}", ToolTipIcon.Warning);
        }

        try { _startupManager.SetEnabled(_settings.StartWithWindows); }
        catch (Exception) { }
    }

    protected override void ExitThreadCore()
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _hotkeyManager.Dispose();
        _hotkeyWindow.Dispose();
        _operationGate.Dispose();
        base.ExitThreadCore();
    }

    private async Task ConvertSelectionAsync()
    {
        if (!_settings.Enabled || !_operationGate.Wait(0)) return;

        ClipboardCopyResult? copyResult = null;
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await _inputSimulator.WaitForHotkeyModifiersToReleaseAsync(cancellation.Token);
            var sourceWindow = NativeMethods.GetForegroundWindow();
            if (_foregroundProcessService.IsExcludedForegroundProcess(_settings.ExcludedProcesses)) return;

            copyResult = await _clipboardService.CopySelectedTextAsync(_inputSimulator, cancellation.Token);
            if (string.IsNullOrEmpty(copyResult.Text)) return;

            var converted = LayoutConverter.Convert(copyResult.Text);
            if (string.Equals(converted, copyResult.Text, StringComparison.Ordinal)) return;
            if (sourceWindow == IntPtr.Zero || NativeMethods.GetForegroundWindow() != sourceWindow) return;
            if (!await _clipboardService.SetUnicodeTextAsync(converted, cancellation.Token)) return;

            _inputSimulator.SendCtrlKey(Keys.V);
            await Task.Delay(125, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            // A timed-out conversion is intentionally ignored.
        }
        catch (Exception)
        {
            // User text and clipboard data must never be logged.
        }
        finally
        {
            if (copyResult is not null)
            {
                try { await _clipboardService.RestoreAsync(copyResult.HasOriginalClipboardSnapshot, copyResult.OriginalClipboard, CancellationToken.None); }
                catch (Exception) { }
            }

            _operationGate.Release();
        }
    }

    private void ToggleEnabled()
    {
        var candidate = _settings.Clone();
        candidate.Enabled = !candidate.Enabled;
        ApplySettings(candidate);
    }

    private void ToggleAutostart()
    {
        var candidate = _settings.Clone();
        candidate.StartWithWindows = !candidate.StartWithWindows;
        ApplySettings(candidate);
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings, ApplySettings);
        form.ShowDialog();
    }

    private (bool Success, string? Error) ApplySettings(AppSettings candidate)
    {
        if (!_hotkeyManager.TryRegister(HotkeyConfiguration.From(candidate), out var hotkeyError))
        {
            return (false, $"Горячая клавиша занята или недоступна: {hotkeyError}");
        }

        try
        {
            _startupManager.SetEnabled(candidate.StartWithWindows);
            _settingsService.Save(candidate);
            _settings = candidate;
            _enabledItem.Checked = candidate.Enabled;
            _autostartItem.Checked = candidate.StartWithWindows;
            return (true, null);
        }
        catch (Exception exception)
        {
            _hotkeyManager.TryRegister(HotkeyConfiguration.From(_settings), out _);
            return (false, exception.Message);
        }
    }
}
