namespace TGWatch.Windows.Core;

public sealed class TalkgroupActivity
{
    public string Network { get; init; } = "";
    public uint Talkgroup { get; init; }
    public string TalkgroupName { get; set; } = "";
    public string Callsign { get; set; } = "";
    public string Node { get; set; } = "";
    public bool Transmitting { get; set; }
    public DateTimeOffset LastActivity { get; set; }
    public DateTimeOffset Started { get; set; }

    public string Key => $"{Network}:{Talkgroup}";
}
