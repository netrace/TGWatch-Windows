using TGWatch.Windows.Config;

namespace TGWatch.Windows.UI;

public sealed class SettingsForm : Form
{
    private readonly CheckBox _ham = new() { Text = "HamThings", AutoSize = true };
    private readonly CheckBox _bm = new() { Text = "BrandMeister", AutoSize = true };
    private readonly CheckBox _hideBmPrivate = new()
    {
        Text = "Nascondi chiamate private BrandMeister",
        AutoSize = true
    };

    private readonly RadioButton _all = new() { Text = "Tutti", AutoSize = true };
    private readonly RadioButton _prefixMode = new() { Text = "Prefisso", AutoSize = true };
    private readonly RadioButton _favoritesMode = new() { Text = "Solo questi TG", AutoSize = true };
    private readonly TextBox _prefix = new() { Width = 155, PlaceholderText = "es. 222" };
    private readonly TextBox _favorites = new() { Width = 230, PlaceholderText = "es. 222, 22221, 91" };

    private readonly NumericUpDown _rows = new() { Minimum = 1, Maximum = 5, Width = 70 };
    private readonly NumericUpDown _ended = new() { Minimum = 5, Maximum = 300, Increment = 5, Width = 70 };
    private readonly TrackBar _opacity = new()
    {
        Minimum = 40,
        Maximum = 100,
        TickFrequency = 10,
        SmallChange = 1,
        LargeChange = 5,
        Width = 180,
        AutoSize = false,
        Height = 32
    };
    private readonly Label _opacityValue = new() { AutoSize = true, Width = 45 };

    private readonly CheckBox _callsign = new() { Text = "Mostra nominativo", AutoSize = true };
    private readonly CheckBox _node = new() { Text = "Mostra nodo/ripetitore", AutoSize = true };
    private readonly CheckBox _startup = new() { Text = "Avvia automaticamente con Windows", AutoSize = true };

    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Text = "TGWatch - Impostazioni";
        ClientSize = new Size(500, 610);
        MinimumSize = new Size(516, 649);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9.5f);

        Result = Clone(current);
        BuildUi();
        LoadValues(current);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        content.Controls.Add(SectionTitle("MONITORAGGIO"));
        content.Controls.Add(Flow(_ham, _bm));
        content.Controls.Add(_hideBmPrivate);

        content.Controls.Add(Spacer());
        content.Controls.Add(SectionTitle("TALKGROUP"));
        content.Controls.Add(BuildFilterPanel());

        content.Controls.Add(Spacer());
        content.Controls.Add(SectionTitle("VISUALIZZAZIONE"));
        content.Controls.Add(Labeled("Righe visibili", _rows));
        content.Controls.Add(Labeled("Mantieni TG terminati (secondi)", _ended));
        content.Controls.Add(Labeled("Trasparenza popup", Flow(_opacity, _opacityValue)));
        content.Controls.Add(_callsign);
        content.Controls.Add(_node);

        content.Controls.Add(Spacer());
        content.Controls.Add(SectionTitle("WINDOWS"));
        content.Controls.Add(_startup);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            Margin = new Padding(0)
        };

        var save = new Button { Text = "Salva", Width = 92, Height = 34 };
        var cancel = new Button { Text = "Annulla", Width = 92, Height = 34, DialogResult = DialogResult.Cancel };

        save.Click += (_, _) => SaveAndClose();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);

        root.Controls.Add(content, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        Controls.Add(root);

        AcceptButton = save;
        CancelButton = cancel;

        _all.CheckedChanged += (_, _) => UpdateEnabledStates();
        _prefixMode.CheckedChanged += (_, _) => UpdateEnabledStates();
        _favoritesMode.CheckedChanged += (_, _) => UpdateEnabledStates();
        _opacity.ValueChanged += (_, _) => _opacityValue.Text = $"{_opacity.Value}%";
    }

    private Control BuildFilterPanel()
    {
        var panel = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Width = 430
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(_all, 0, 0);
        panel.SetColumnSpan(_all, 2);
        panel.Controls.Add(_prefixMode, 0, 1);
        panel.Controls.Add(_prefix, 1, 1);
        panel.Controls.Add(_favoritesMode, 0, 2);
        panel.Controls.Add(_favorites, 1, 2);

        return panel;
    }

    private void LoadValues(AppSettings s)
    {
        _ham.Checked = s.HamThingsEnabled;
        _bm.Checked = s.BrandMeisterEnabled;
        _hideBmPrivate.Checked = s.HideBrandMeisterPrivateCalls;
        _prefix.Text = s.Prefix;
        _favorites.Text = string.Join(", ", s.Favorites);
        _rows.Value = Math.Clamp(s.MaxRows, 1, 5);
        _ended.Value = Math.Clamp(s.EndedSeconds, 5, 300);
        _opacity.Value = Math.Clamp(s.PopupOpacityPercent, 40, 100);
        _opacityValue.Text = $"{_opacity.Value}%";
        _callsign.Checked = s.ShowCallsign;
        _node.Checked = s.ShowNode;
        _startup.Checked = s.StartWithWindows;

        _all.Checked = s.FilterMode == TalkgroupFilterMode.All;
        _prefixMode.Checked = s.FilterMode == TalkgroupFilterMode.Prefix;
        _favoritesMode.Checked = s.FilterMode == TalkgroupFilterMode.Favorites;
        UpdateEnabledStates();
    }

    private void UpdateEnabledStates()
    {
        _prefix.Enabled = _prefixMode.Checked;
        _favorites.Enabled = _favoritesMode.Checked;
    }

    private void SaveAndClose()
    {
        if (!_ham.Checked && !_bm.Checked)
        {
            MessageBox.Show(this, "Attiva almeno una rete.", "TGWatch",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var prefix = _prefix.Text.Trim();
        if (_prefixMode.Checked && string.IsNullOrWhiteSpace(prefix))
        {
            MessageBox.Show(this, "Inserisci un prefisso TG.",
                "TGWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_prefixMode.Checked && !prefix.All(char.IsDigit))
        {
            MessageBox.Show(this, "Il prefisso deve contenere solo cifre.",
                "TGWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var favorites = new List<uint>();
        if (_favoritesMode.Checked)
        {
            var parts = _favorites.Text.Split(
                new[] { ',', ';', ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (!uint.TryParse(part, out var tg) || tg == 0)
                {
                    MessageBox.Show(this, $"TG non valido: {part}", "TGWatch",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!favorites.Contains(tg))
                    favorites.Add(tg);
            }

            if (favorites.Count == 0)
            {
                MessageBox.Show(this, "Inserisci almeno un TG nella lista.",
                    "TGWatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        Result = new AppSettings
        {
            HamThingsEnabled = _ham.Checked,
            BrandMeisterEnabled = _bm.Checked,
            HideBrandMeisterPrivateCalls = _hideBmPrivate.Checked,
            FilterMode = _prefixMode.Checked ? TalkgroupFilterMode.Prefix :
                         _favoritesMode.Checked ? TalkgroupFilterMode.Favorites :
                         TalkgroupFilterMode.All,
            Prefix = prefix,
            Favorites = favorites,
            MaxRows = (int)_rows.Value,
            EndedSeconds = (int)_ended.Value,
            PopupOpacityPercent = _opacity.Value,
            ShowCallsign = _callsign.Checked,
            ShowNode = _node.Checked,
            StartWithWindows = _startup.Checked,
            Paused = Result.Paused
        };

        DialogResult = DialogResult.OK;
        Close();
    }

    private static Label SectionTitle(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = Color.FromArgb(80, 80, 85),
        Margin = new Padding(0, 4, 0, 8)
    };

    private static Control Spacer() => new Panel { Height = 12, Width = 1 };

    private static FlowLayoutPanel Flow(params Control[] controls)
    {
        var p = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 2, 0, 2)
        };
        p.Controls.AddRange(controls);
        return p;
    }

    private static FlowLayoutPanel Labeled(string label, Control control)
    {
        var l = new Label
        {
            Text = label,
            AutoSize = false,
            Width = 245,
            Height = 30,
            Padding = new Padding(0, 5, 0, 0)
        };
        return Flow(l, control);
    }

    private static AppSettings Clone(AppSettings s) => new()
    {
        HamThingsEnabled = s.HamThingsEnabled,
        BrandMeisterEnabled = s.BrandMeisterEnabled,
        HideBrandMeisterPrivateCalls = s.HideBrandMeisterPrivateCalls,
        FilterMode = s.FilterMode,
        Prefix = s.Prefix,
        Favorites = [.. s.Favorites],
        MaxRows = s.MaxRows,
        EndedSeconds = s.EndedSeconds,
        PopupOpacityPercent = s.PopupOpacityPercent,
        ShowCallsign = s.ShowCallsign,
        ShowNode = s.ShowNode,
        StartWithWindows = s.StartWithWindows,
        Paused = s.Paused
    };
}
