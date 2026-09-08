namespace TGWatch.Windows.Core;

public sealed record FeedActivityEvent(
    string Network,
    uint Talkgroup,
    string TalkgroupName,
    string Callsign,
    string Node,
    bool Transmitting,
    DateTimeOffset Timestamp,
    DateTimeOffset Started
);
