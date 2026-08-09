using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace KeyFlip;

public sealed class KeyFlipContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private readonly StartupManager _startupManager = new();
    private readonly ForegroundProcessService _foregroundProcessService = new();
    private readonly InputSimulator _inputSimulator = new();
    private readonly ClipboardService _clipboardService = new();
    private readonly ProtectedFieldDetector _protectedFieldDetector = new();
    private readonly DiagnosticLogger _logger = new();
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
        _hotkeyWindow.HotkeyPressed += (_, _) =>
        {
            _logger.Log("HOTKEY_RECEIVED");
            _ = ConvertSelectionAsync();
        };

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

        var hotkeyRegistered = _hotkeyManager.TryRegister(HotkeyConfiguration.From(_settings), out var error);
        _logger.Log("STARTUP", $"architecture={(Environment.Is64BitProcess ? "x64" : "x86")} inputSize={Marshal.SizeOf<NativeMethods.Input>()} hotkeyRegistered={(hotkeyRegistered ? "yes" : "no")}");
        if (!hotkeyRegistered)
        {
            _trayIcon.ShowBalloonTip(3000, "KeyFlip", $"Не удалось зарегистрировать горячую клавишу: {error}", ToolTipIcon.Warning);
        }

        try { _startupManager.SetEnabled(_settings.StartWithWindows); }
        catch (Exception exception) { _logger.Log("STARTUP_AUTOSTART_FAILED", $"exception={exception.GetType().Name}"); }
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
        if (!_settings.Enabled)
        {
            _logger.Log("HOTKEY_IGNORED_DISABLED");
            return;
        }

        if (!_operationGate.Wait(0))
        {
            _logger.Log("OPERATION_BUSY");
            return;
        }

        ClipboardSnapshot? clipboardSnapshot = null;
        var operationCompleted = false;
        var clipboardRestored = false;
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            if (!await _inputSimulator.WaitForHotkeyModifiersToReleaseAsync(cancellation.Token))
            {
                _logger.Log("MODIFIERS_TIMEOUT");
                return;
            }

            _logger.Log("MODIFIERS_RELEASED");
            var sourceWindow = NativeMethods.GetForegroundWindow();
            var foregroundExecutable = _foregroundProcessService.GetForegroundExecutableName();
            if (sourceWindow == IntPtr.Zero || ForegroundProcessService.IsExcludedProcessName(foregroundExecutable, _settings.ExcludedProcesses))
            {
                _logger.Log("FOREGROUND_REJECTED", $"executable={foregroundExecutable}");
                return;
            }

            _logger.Log("FOREGROUND_ACCEPTED", $"executable={foregroundExecutable} hwnd=0x{sourceWindow.ToInt64():X}");
            if (_protectedFieldDetector.IsFocusedControlProtected())
            {
                _logger.Log("PROTECTED_FIELD_ABORT");
                return;
            }

            clipboardSnapshot = await _clipboardService.CaptureSnapshotAsync(_logger, cancellation.Token);
            if (clipboardSnapshot is null) return;

            var copyResult = await _clipboardService.CopySelectedTextAsync(_inputSimulator, _logger, cancellation.Token);
            if (string.IsNullOrWhiteSpace(copyResult.Text))
            {
                _logger.Log("NO_TEXT", $"clipboardChanged={(copyResult.SequenceChanged ? "yes" : "no")}");
                return;
            }

            _logger.Log("TEXT_AVAILABLE", $"clipboardChanged={(copyResult.SequenceChanged ? "yes" : "no")}");

            var converted = LayoutConverter.Convert(copyResult.Text);
            if (!ConversionGuard.CanPaste(copyResult.Text, converted))
            {
                _logger.Log("CONVERSION_UNCHANGED");
                return;
            }

            _logger.Log("CONVERSION_READY");
            if (NativeMethods.GetForegroundWindow() != sourceWindow)
            {
                _logger.Log("FOREGROUND_CHANGED");
                return;
            }

            if (!await _clipboardService.SetUnicodeTextAsync(converted, cancellation.Token))
            {
                _logger.Log("PASTE_CLIPBOARD_SET_FAILED");
                return;
            }

            _logger.Log("PASTE_CLIPBOARD_SET");
            _logger.Log("TEMP_CLIPBOARD_READY");

            _inputSimulator.SendCtrlKey(Keys.V, SendInputOperation.Paste);
            _logger.Log("PASTE_SENT");
            _logger.Log("SYNTHETIC_MODIFIERS_RELEASED");
            await Task.Delay(300, cancellation.Token);
            operationCompleted = true;
        }
        catch (OperationCanceledException)
        {
            _logger.Log("OPERATION_TIMEOUT");
        }
        catch (SendInputException exception)
        {
            var stage = exception.IsCleanup
                ? "SYNTHETIC_CTRL_RELEASE_FAILED"
                : exception.Operation == SendInputOperation.Copy ? "COPY_SENDINPUT_FAILED" : "PASTE_SENDINPUT_FAILED";
            _logger.Log(stage, exception.ToDiagnosticMetadata());
        }
        catch (PhysicalModifierPressedException exception)
        {
            _logger.Log(exception.Operation == SendInputOperation.Copy ? "COPY_MODIFIER_REAPPEARED" : "PASTE_MODIFIER_REAPPEARED");
        }
        catch (Exception exception)
        {
            _logger.Log("EXCEPTION", $"exception={exception.GetType().Name}");
        }
        finally
        {
            if (clipboardSnapshot is not null)
            {
                try
                {
                    clipboardRestored = await _clipboardService.RestoreAsync(clipboardSnapshot, _logger, CancellationToken.None);
                }
                catch (Exception exception) { _logger.Log("CLIPBOARD_RESTORE_FAILED", $"exception={exception.GetType().Name}"); }
                finally { clipboardSnapshot.Dispose(); }
            }

            if (operationCompleted && clipboardRestored) _logger.Log("TRANSACTION_COMPLETE");
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
