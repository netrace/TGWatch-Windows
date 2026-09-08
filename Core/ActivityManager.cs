using TGWatch.Windows.Config;

namespace TGWatch.Windows.Core;

public sealed class ActivityManager
{
    private readonly Dictionary<string, TalkgroupActivity> _items = new();
    private readonly object _gate = new();

    public event Action? Changed;

    public void Clear()
    {
        lock (_gate) _items.Clear();
        Changed?.Invoke();
    }

    public void Upsert(FeedActivityEvent e, AppSettings settings)
    {
        if (!MatchesFilter(e.Talkgroup, settings))
            return;

        lock (_gate)
        {
            var key = $"{e.Network}:{e.Talkgroup}";
            if (!_items.TryGetValue(key, out var item))
            {
                item = new TalkgroupActivity
                {
                    Network = e.Network,
                    Talkgroup = e.Talkgroup
                };
                _items[key] = item;
            }

            item.TalkgroupName = e.TalkgroupName;
            item.Callsign = e.Callsign;
            item.Node = e.Node;
            item.Transmitting = e.Transmitting;
            item.LastActivity = e.Timestamp;
            item.Started = e.Started;
        }

        Changed?.Invoke();
    }

    public IReadOnlyList<TalkgroupActivity> Snapshot(AppSettings settings)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-Math.Clamp(settings.EndedSeconds, 5, 300));

        lock (_gate)
        {
            var stale = _items
                .Where(kv => !kv.Value.Transmitting && kv.Value.LastActivity < cutoff)
                .Select(kv => kv.Key)
                .ToArray();

            foreach (var key in stale)
                _items.Remove(key);

            return _items.Values
                .Where(x => (x.Transmitting || x.LastActivity >= cutoff)
                            && MatchesFilter(x.Talkgroup, settings))
                .OrderByDescending(x => x.Transmitting)
                .ThenBy(x => x.Talkgroup)
                .Take(Math.Clamp(settings.MaxRows, 1, 5))
                .Select(Clone)
                .ToArray();
        }
    }

    private static TalkgroupActivity Clone(TalkgroupActivity x) => new()
    {
        Network = x.Network,
        Talkgroup = x.Talkgroup,
        TalkgroupName = x.TalkgroupName,
        Callsign = x.Callsign,
        Node = x.Node,
        Transmitting = x.Transmitting,
        LastActivity = x.LastActivity,
        Started = x.Started
    };

    public static bool MatchesFilter(uint tg, AppSettings settings)
    {
        return settings.FilterMode switch
        {
            TalkgroupFilterMode.All => true,
            TalkgroupFilterMode.Prefix =>
                !string.IsNullOrWhiteSpace(settings.Prefix) &&
                tg.ToString().StartsWith(settings.Prefix.Trim(), StringComparison.Ordinal),
            TalkgroupFilterMode.Favorites =>
                settings.Favorites.Count > 0 && settings.Favorites.Contains(tg),
            _ => true
        };
    }
}
