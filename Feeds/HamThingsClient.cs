using System.Text.Json;
using TGWatch.Windows.Core;

namespace TGWatch.Windows.Feeds;

public sealed class HamThingsClient : SocketIoFeedClient
{
    public override string Name => "HamThings";

    public HamThingsClient()
        : base(
            "wss://www.hamthings.it/socket.io/?EIO=4&transport=websocket",
            "https://www.hamthings.it",
            "42[\"join\",{}]")
    {
    }

    protected override FeedActivityEvent? ParseCall(JsonElement call, bool startup, string callEvent)
    {
        var callType = (int)GetUInt(call, "CallType");
        if (callType is not (7 or 11))
            return null;

        var tg = GetUInt(call, "DestinationID");
        if (tg == 0) return null;

        var update = GetUInt(call, "UpdateTime");
        var creation = GetUInt(call, "CreationTime");
        var stamp = update != 0 ? update : creation;

        bool transmitting = false;
        if (!startup)
        {
            var finalEvent = callEvent.Equals("Final", StringComparison.OrdinalIgnoreCase);
            int state = -1;

            if (call.TryGetProperty("State", out var stateElement))
            {
                if (stateElement.ValueKind == JsonValueKind.Number)
                    stateElement.TryGetInt32(out state);
                else
                    int.TryParse(stateElement.ToString(), out state);
            }

            var ended = state >= 0 && state <= 2;
            transmitting = !finalEvent && !ended &&
                           (callEvent.Equals("Intermediate", StringComparison.OrdinalIgnoreCase) || state > 2);
        }

        return new FeedActivityEvent(
            Name,
            tg,
            GetString(call, "DestinationName"),
            GetString(call, "SourceCall"),
            GetString(call, "RepeaterCall"),
            transmitting,
            Unix(stamp),
            Unix(creation)
        );
    }
}
