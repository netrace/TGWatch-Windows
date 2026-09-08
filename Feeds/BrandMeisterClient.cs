using System.Text.Json;
using TGWatch.Windows.Core;

namespace TGWatch.Windows.Feeds;

public sealed class BrandMeisterClient : SocketIoFeedClient
{
    public override string Name => "BrandMeister";
    private readonly bool _hidePrivateCalls;

    public BrandMeisterClient(bool hidePrivateCalls = true)
        : base(
            "wss://api.brandmeister.network/lh/?EIO=4&transport=websocket",
            "https://brandmeister.network",
            "42[\"join\",\"everything\"]")
    {
        _hidePrivateCalls = hidePrivateCalls;
    }

    protected override FeedActivityEvent? ParseCall(JsonElement call, bool startup, string callEvent)
    {
        if (!IsBrandMeisterGroupVoice(call, _hidePrivateCalls))
            return null;

        var tg = GetUInt(call, "DestinationID");
        if (tg == 0) return null;

        var start = GetUInt(call, "Start");
        var stop = GetUInt(call, "Stop");
        var eventName = GetString(call, "Event");

        uint stamp;
        if (stop != 0)
            stamp = stop;
        else
            stamp = start;

        var transmitting = !startup && stop == 0 &&
            (eventName == "Session-Start" || eventName == "Session-Update");

        var node = GetString(call, "LinkCall");
        if (string.IsNullOrWhiteSpace(node))
            node = GetString(call, "LinkName");

        return new FeedActivityEvent(
            Name,
            tg,
            GetString(call, "DestinationName"),
            GetString(call, "SourceCall"),
            node,
            transmitting,
            Unix(stamp),
            Unix(start)
        );
    }
}
