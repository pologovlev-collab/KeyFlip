using System.Windows.Forms;

namespace KeyFlip;

internal sealed class SettingsForm : Form
{
    private readonly AppSettings _settings;
    private readonly Func<AppSettings, (bool Success, string? Error)> _apply;
    private readonly CheckBox _enabled = new() { Text = "Включено", AutoSize = true };
    private readonly ComboBox _modifiers = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _key = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _autostart = new() { Text = "Запускать KeyFlip вместе с Windows", AutoSize = true };
    private readonly TextBox _excluded = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 120, Dock = DockStyle.Fill };

    public SettingsForm(AppSettings settings, Func<AppSettings, (bool Success, string? Error)> apply)
    {
        _settings = settings.Clone();
        _apply = apply;
        Text = "KeyFlip — Настройки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, 330);

        _modifiers.Items.AddRange(new object[]
        {
            HotkeyModifiers.Control,
            HotkeyModifiers.Control | HotkeyModifiers.Alt,
            HotkeyModifiers.Control | HotkeyModifiers.Shift,
            HotkeyModifiers.Alt | HotkeyModifiers.Shift,
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift
        });
        _key.Items.AddRange(Enumerable.Range((int)Keys.A, 26).Select(value => (Keys)value)
            .Concat(Enumerable.Range((int)Keys.D0, 10).Select(value => (Keys)value)).Cast<object>().ToArray());
        _enabled.Checked = _settings.Enabled;
        _modifiers.SelectedItem = _settings.HotkeyModifiers;
        _key.SelectedItem = (Keys)_settings.HotkeyVirtualKey;
        _autostart.Checked = _settings.StartWithWindows;
        _excluded.Text = string.Join(Environment.NewLine, _settings.ExcludedProcesses);

        var save = new Button { Text = "Сохранить", AutoSize = true };
        save.Click += (_, _) => Save();
        var cancel = new Button { Text = "Отмена", AutoSize = true, DialogResult = DialogResult.Cancel };
        AcceptButton = save;
        CancelButton = cancel;

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 6 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(_enabled, 0, 0);
        layout.SetColumnSpan(_enabled, 2);
        layout.Controls.Add(new Label { Text = "Модификаторы:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        layout.Controls.Add(_modifiers, 1, 1);
        layout.Controls.Add(new Label { Text = "Клавиша:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        layout.Controls.Add(_key, 1, 2);
        layout.Controls.Add(_autostart, 0, 3);
        layout.SetColumnSpan(_autostart, 2);
        layout.Controls.Add(new Label { Text = "Исключённые процессы (.exe):", AutoSize = true }, 0, 4);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 4)!, 2);
        layout.Controls.Add(_excluded, 0, 5);
        layout.SetColumnSpan(_excluded, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 42, Padding = new Padding(12, 6, 12, 6) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        Controls.Add(layout);
        Controls.Add(buttons);
    }

    private void Save()
    {
        var candidate = _settings.Clone();
        candidate.Enabled = _enabled.Checked;
        candidate.HotkeyModifiers = (HotkeyModifiers)_modifiers.SelectedItem!;
        candidate.HotkeyVirtualKey = (int)(Keys)_key.SelectedItem!;
        candidate.StartWithWindows = _autostart.Checked;
        candidate.ExcludedProcesses = _excluded.Lines
            .SelectMany(line => line.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = _apply(candidate);
        if (!result.Success)
        {
            MessageBox.Show(this, result.Error ?? "Не удалось сохранить настройки.", "KeyFlip", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
