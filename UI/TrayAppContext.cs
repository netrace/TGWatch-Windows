using System.Drawing;
using TGWatch.Windows.Config;
using TGWatch.Windows.Core;
using TGWatch.Windows.Feeds;
using TGWatch.Windows.Windows;

namespace TGWatch.Windows.UI;

public sealed class TrayAppContext : ApplicationContext
{
    private readonly NotifyIcon _tray;
    private readonly ActivityPopup _popup = new();
    private readonly ActivityManager _activity = new();
    private readonly SynchronizationContext _ui;
    private readonly Icon _appIcon;

    private AppSettings _settings;
    private CancellationTokenSource? _feedCts;
    private readonly List<IFeedClient> _feeds = [];

    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private string _lastPopupSignature = "";

    private readonly Dictionary<string, bool> _connections = new()
    {
        ["HamThings"] = false,
        ["BrandMeister"] = false
    };

    public TrayAppContext()
    {
        _ui = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();
        _settings = SettingsStore.Load();

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "TGWatch.ico");
        _appIcon = File.Exists(iconPath)
            ? new Icon(iconPath)
            : Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? string.Empty) ?? SystemIcons.Application;

        _pauseItem = new ToolStripMenuItem("Pausa monitoraggio")
        {
            CheckOnClick = true,
            Checked = _settings.Paused
        };
        _pauseItem.CheckedChanged += (_, _) =>
        {
            _settings.Paused = _pauseItem.Checked;
            SettingsStore.Save(_settings);

            if (_settings.Paused)
            {
                StopFeeds();
                _activity.Clear();
            }
            else
            {
                StartFeeds();
            }

            UpdateTrayText();
        };

        _startupItem = new ToolStripMenuItem("Avvia con Windows")
        {
            CheckOnClick = true,
            Checked = _settings.StartWithWindows
        };
        _startupItem.CheckedChanged += (_, _) =>
        {
            _settings.StartWithWindows = _startupItem.Checked;
            StartupManager.SetEnabled(_settings.StartWithWindows);
            SettingsStore.Save(_settings);
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Mostra attività", null, (_, _) => RefreshPopup(force: true));
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Impostazioni...", null, (_, _) => ShowSettings());
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Esci", null, (_, _) => Exit());

        _tray = new NotifyIcon
        {
            Visible = true,
            Icon = _appIcon,
            Text = "TGWatch",
            ContextMenuStrip = menu
        };

        _tray.DoubleClick += (_, _) => ShowSettings();

        _activity.Changed += () => _ui.Post(_ => RefreshPopup(), null);

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _refreshTimer.Tick += (_, _) => RefreshPopup();
        _refreshTimer.Start();

        StartupManager.SetEnabled(_settings.StartWithWindows);

        if (!_settings.Paused)
            StartFeeds();

        UpdateTrayText();
    }

    private void StartFeeds()
    {
        StopFeeds();

        _feedCts = new CancellationTokenSource();
        _feeds.Clear();

        if (_settings.HamThingsEnabled)
            _feeds.Add(new HamThingsClient());

        if (_settings.BrandMeisterEnabled)
            _feeds.Add(new BrandMeisterClient(_settings.HideBrandMeisterPrivateCalls));

        foreach (var feed in _feeds)
        {
            feed.Activity += OnFeedActivity;
            feed.ConnectionChanged += OnConnectionChanged;
            _ = Task.Run(() => feed.StartAsync(_feedCts.Token));
        }
    }

    private void StopFeeds()
    {
        try { _feedCts?.Cancel(); } catch { }

        foreach (var feed in _feeds)
        {
            feed.Activity -= OnFeedActivity;
            feed.ConnectionChanged -= OnConnectionChanged;
            try { _ = feed.DisposeAsync(); } catch { }
        }

        _feeds.Clear();
        _feedCts?.Dispose();
        _feedCts = null;

        _connections["HamThings"] = false;
        _connections["BrandMeister"] = false;
    }

    private void OnFeedActivity(FeedActivityEvent e)
    {
        if (_settings.Paused) return;
        _ui.Post(_ => _activity.Upsert(e, _settings), null);
    }

    private void OnConnectionChanged(string name, bool connected)
    {
        _ui.Post(_ =>
        {
            _connections[name] = connected;
            UpdateTrayText();
        }, null);
    }

    private void RefreshPopup(bool force = false)
    {
        if (_settings.Paused)
        {
            _lastPopupSignature = "";
            _popup.Hide();
            return;
        }

        var items = _activity.Snapshot(_settings);
        var signature = BuildPopupSignature(items);

        if (force || signature != _lastPopupSignature)
        {
            _lastPopupSignature = signature;
            _popup.Render(items, _settings);
        }

        if (force && items.Count == 0)
        {
            _tray.ShowBalloonTip(1800, "TGWatch", "Nessun TG attivo in questo momento.",
                ToolTipIcon.Info);
        }
    }

    private string BuildPopupSignature(IReadOnlyList<TalkgroupActivity> items)
    {
        var parts = items.Select(x =>
            $"{x.Key}|{x.Transmitting}|{x.TalkgroupName}|{x.Callsign}|{x.Node}|{x.LastActivity.UtcDateTime.Ticks}");

        return $"{_settings.MaxRows};{_settings.ShowCallsign};{_settings.ShowNode};{_settings.PopupOpacityPercent};"
               + string.Join(";;", parts);
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings) { Icon = _appIcon };
        if (form.ShowDialog() != DialogResult.OK)
            return;

        var oldStartup = _settings.StartWithWindows;
        _settings = form.Result;
        SettingsStore.Save(_settings);

        if (oldStartup != _settings.StartWithWindows)
            StartupManager.SetEnabled(_settings.StartWithWindows);

        _startupItem.Checked = _settings.StartWithWindows;
        _pauseItem.Checked = _settings.Paused;

        _lastPopupSignature = "";
        _activity.Clear();
        if (!_settings.Paused)
            StartFeeds();

        UpdateTrayText();
    }

    private void UpdateTrayText()
    {
        string text;

        if (_settings.Paused)
        {
            text = "TGWatch - in pausa";
        }
        else
        {
            var connected = _connections.Where(x => x.Value).Select(x => x.Key).ToArray();

            text = connected.Length == 0
                ? "TGWatch - connessione..."
                : $"TGWatch - {string.Join(" + ", connected)}";
        }

        var filterText = _settings.FilterMode switch
        {
            TalkgroupFilterMode.Prefix => $" | prefisso {_settings.Prefix}",
            TalkgroupFilterMode.Favorites => $" | TG {string.Join(",", _settings.Favorites)}",
            _ => " | tutti i TG"
        };

        text += filterText;
        _tray.Text = text.Length > 63 ? text[..63] : text;
    }

    private void Exit()
    {
        _refreshTimer.Stop();
        StopFeeds();
        _popup.Close();
        _tray.Visible = false;
        _tray.Dispose();
        _appIcon.Dispose();
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer.Dispose();
            StopFeeds();
            _popup.Dispose();
            _tray.Dispose();
            _appIcon.Dispose();
        }

        base.Dispose(disposing);
    }
}
