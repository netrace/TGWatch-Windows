using System.Runtime.InteropServices;
using TGWatch.Windows.Config;
using TGWatch.Windows.Core;

namespace TGWatch.Windows.UI;

public sealed class ActivityPopup : Form
{
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WM_NCLBUTTONDOWN = 0x00A1;
    private const int HTCAPTION = 0x0002;

    private readonly TableLayoutPanel _table = new();
    private AppSettings _settings = new();
    private bool _hasCustomPosition;

    public ActivityPopup()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        BackColor = Color.FromArgb(30, 30, 32);
        Padding = new Padding(10);
        Width = 360;

        _table.Dock = DockStyle.Fill;
        _table.ColumnCount = 1;
        _table.AutoSize = true;
        _table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _table.BackColor = BackColor;
        Controls.Add(_table);

        AttachDragHandler(this);
        AttachDragHandler(_table);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    public void Render(IReadOnlyList<TalkgroupActivity> items, AppSettings settings)
    {
        _settings = settings;
        Opacity = Math.Clamp(settings.PopupOpacityPercent, 40, 100) / 100.0;

        if (items.Count == 0)
        {
            Hide();
            return;
        }

        SuspendLayout();
        _table.SuspendLayout();

        try
        {
            _table.Controls.Clear();
            _table.RowStyles.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                var row = BuildRow(items[i]);
                _table.Controls.Add(row, 0, i);
                _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 61));
            }

            Height = Math.Max(58, 18 + items.Count * 62);

            if (!_hasCustomPosition)
                PositionNearTray();

            ShowInactiveTopmost();
        }
        finally
        {
            _table.ResumeLayout(true);
            ResumeLayout(true);
        }
    }

    private Control BuildRow(TalkgroupActivity item)
    {
        var networkAccent = item.Network == "BrandMeister"
            ? Color.FromArgb(230, 145, 55)
            : Color.FromArgb(70, 150, 235);

        var activeBackground = item.Network == "BrandMeister"
            ? Color.FromArgb(58, 45, 34)
            : Color.FromArgb(31, 45, 61);

        var inactiveBackground = item.Network == "BrandMeister"
            ? Color.FromArgb(43, 38, 33)
            : Color.FromArgb(33, 39, 46);

        var panel = new Panel
        {
            Height = 58,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 3),
            Padding = new Padding(5, 3, 5, 3),
            BackColor = item.Transmitting ? activeBackground : inactiveBackground
        };

        var networkBar = new Panel
        {
            BackColor = networkAccent,
            Location = new Point(0, 0),
            Size = new Size(4, 58),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
        };

        var indicator = new Label
        {
            AutoSize = true,
            Text = item.Transmitting ? "●" : " ",
            ForeColor = networkAccent,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(9, 6)
        };

        var title = new Label
        {
            AutoEllipsis = true,
            Text = BuildTitle(item),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Location = new Point(32, 5),
            Size = new Size(296, 22)
        };

        var detail = new Label
        {
            AutoEllipsis = true,
            Text = BuildDetail(item),
            ForeColor = item.Transmitting
                ? Color.FromArgb(210, 215, 215)
                : Color.FromArgb(145, 145, 150),
            Font = new Font("Segoe UI", 8.5f),
            Location = new Point(32, 29),
            Size = new Size(296, 20)
        };

        panel.Controls.Add(networkBar);
        panel.Controls.Add(indicator);
        panel.Controls.Add(title);
        panel.Controls.Add(detail);
        return panel;
    }

    private string BuildTitle(TalkgroupActivity item)
    {
        var name = string.IsNullOrWhiteSpace(item.TalkgroupName) ? "TG" : item.TalkgroupName;
        return $"{item.Talkgroup}  {name}";
    }

    private string BuildDetail(TalkgroupActivity item)
    {
        var bits = new List<string>();

        if (_settings.ShowCallsign && !string.IsNullOrWhiteSpace(item.Callsign))
            bits.Add(item.Callsign);

        if (_settings.ShowNode && !string.IsNullOrWhiteSpace(item.Node))
            bits.Add($"via {item.Node}");

        bits.Add(item.Network == "BrandMeister" ? "BM" : "HT");
        return string.Join("  ·  ", bits);
    }

    private void AttachDragHandler(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
                return;

            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            _hasCustomPosition = true;
            ClampToScreen();
        };
    }

    private void PositionNearTray()
    {
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Left = area.Right - Width - 12;
        Top = area.Bottom - Height - 12;
    }

    private void ClampToScreen()
    {
        var area = Screen.FromRectangle(Bounds).WorkingArea;
        Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - Width));
        Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - Height));
    }

    private void ShowInactiveTopmost()
    {
        if (!Visible)
            Show();

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HWND_TOPMOST,
            Left, Top, Width, Height,
            NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_SHOWWINDOW);
    }

    private static class NativeMethods
    {
        public static readonly IntPtr HWND_TOPMOST = new(-1);
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);
    }
}
