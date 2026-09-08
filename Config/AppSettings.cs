namespace TGWatch.Windows.Config;

public enum TalkgroupFilterMode
{
    All,
    Prefix,
    Favorites
}

public sealed class AppSettings
{
    public bool HamThingsEnabled { get; set; } = true;
    public bool BrandMeisterEnabled { get; set; } = true;
    public bool HideBrandMeisterPrivateCalls { get; set; } = true;

    public TalkgroupFilterMode FilterMode { get; set; } = TalkgroupFilterMode.All;
    public string Prefix { get; set; } = "";
    public List<uint> Favorites { get; set; } = [];

    public int MaxRows { get; set; } = 3;
    public int EndedSeconds { get; set; } = 30;
    public int PopupOpacityPercent { get; set; } = 92;

    public bool ShowCallsign { get; set; } = true;
    public bool ShowNode { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public bool Paused { get; set; } = false;
}
