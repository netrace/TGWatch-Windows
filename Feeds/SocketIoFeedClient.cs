using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using TGWatch.Windows.Core;

namespace TGWatch.Windows.Feeds;

/// <summary>
/// Minimal Engine.IO v4 / Socket.IO client tailored to the two public TGWatch feeds.
/// No external NuGet packages are required.
/// </summary>
public abstract class SocketIoFeedClient : IFeedClient
{
    private readonly Uri _uri;
    private readonly string _origin;
    private readonly string _joinMessage;
    private ClientWebSocket? _socket;

    public abstract string Name { get; }
    public bool IsConnected { get; private set; }

    public event Action<FeedActivityEvent>? Activity;
    public event Action<string, bool>? ConnectionChanged;

    protected SocketIoFeedClient(string url, string origin, string joinMessage)
    {
        _uri = new Uri(url);
        _origin = origin;
        _joinMessage = joinMessage;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch
            {
                SetConnected(false);
                try { await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        _socket?.Dispose();
        _socket = new ClientWebSocket();
        _socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        _socket.Options.SetRequestHeader("Origin", _origin);
        _socket.Options.SetRequestHeader("User-Agent", "TGWatch-Windows/0.1");

        await _socket.ConnectAsync(_uri, ct);

        var buffer = new byte[64 * 1024];

        while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var text = await ReceiveTextAsync(_socket, buffer, ct);
            if (text is null) break;

            if (text.StartsWith("0", StringComparison.Ordinal))
            {
                await SendAsync("40", ct);
                continue;
            }

            if (text == "2")
            {
                await SendAsync("3", ct);
                continue;
            }

            if (text.StartsWith("40", StringComparison.Ordinal))
            {
                SetConnected(true);
                await SendAsync(_joinMessage, ct);
                continue;
            }

            if (text == "1" || text.StartsWith("41", StringComparison.Ordinal))
                break;

            if (!text.StartsWith("42", StringComparison.Ordinal))
                continue;

            TryHandleSocketIoEvent(text.AsSpan(2));
        }

        SetConnected(false);
    }

    private void TryHandleSocketIoEvent(ReadOnlySpan<char> json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json.ToString());
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2)
                return;

            if (root[0].GetString() != "mqtt")
                return;

            var wrapper = root[1];
            var topic = GetString(wrapper, "topic");
            var callEvent = GetString(wrapper, "callEvent");

            if (!wrapper.TryGetProperty("payload", out var payload))
                return;

            JsonDocument? inner = null;
            JsonElement call;

            if (payload.ValueKind == JsonValueKind.String)
            {
                var payloadText = payload.GetString();
                if (string.IsNullOrWhiteSpace(payloadText)) return;
                inner = JsonDocument.Parse(payloadText);
                call = inner.RootElement;
            }
            else
            {
                call = payload;
            }

            try
            {
                var parsed = ParseCall(call, topic == "LH-Startup", callEvent);
                if (parsed is not null)
                    Activity?.Invoke(parsed);
            }
            finally
            {
                inner?.Dispose();
            }
        }
        catch
        {
            // Public feeds occasionally contain incomplete events.
            // Ignore one malformed packet and keep the live connection.
        }
    }

    protected abstract FeedActivityEvent? ParseCall(JsonElement call, bool startup, string callEvent);

    protected static string GetString(JsonElement e, string property)
    {
        if (!e.TryGetProperty(property, out var p)) return "";
        return p.ValueKind == JsonValueKind.String ? p.GetString() ?? "" : p.ToString();
    }

    protected static uint GetUInt(JsonElement e, string property)
    {
        if (!e.TryGetProperty(property, out var p)) return 0;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetUInt32(out var v)) return v;
        return uint.TryParse(p.ToString(), out v) ? v : 0;
    }

    protected static DateTimeOffset Unix(uint value)
    {
        if (value == 0) return DateTimeOffset.UtcNow;
        try { return DateTimeOffset.FromUnixTimeSeconds(value); }
        catch { return DateTimeOffset.UtcNow; }
    }

    protected static bool IsBrandMeisterGroupVoice(JsonElement call, bool rejectPrivateCalls)
    {
        if (!call.TryGetProperty("CallTypes", out var types) || types.ValueKind != JsonValueKind.Array)
            return false;

        bool group = false;
        bool voice = false;
        bool privateCall = false;

        foreach (var t in types.EnumerateArray())
        {
            var value = t.GetString() ?? "";

            if (value.Equals("Group", StringComparison.OrdinalIgnoreCase))
                group = true;

            if (value.Equals("Voice", StringComparison.OrdinalIgnoreCase))
                voice = true;

            // Some BrandMeister Last Heard events can carry more than one
            // classification token. A packet containing Group + Voice + Private
            // must not be accepted merely because Group and Voice are present.
            if (value.Equals("Private", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Individual", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Unit", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("UnitToUnit", StringComparison.OrdinalIgnoreCase))
            {
                privateCall = true;
            }
        }

        return group && voice && (!rejectPrivateCalls || !privateCall);
    }

    private async Task SendAsync(string text, CancellationToken ct)
    {
        if (_socket?.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(text);
        await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }

    private static async Task<string?> ReceiveTextAsync(ClientWebSocket socket, byte[] buffer, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
                return Encoding.UTF8.GetString(ms.ToArray());

            if (ms.Length > 1024 * 1024)
                return null;
        }
    }

    private void SetConnected(bool connected)
    {
        if (IsConnected == connected) return;
        IsConnected = connected;
        ConnectionChanged?.Invoke(Name, connected);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket?.State == WebSocketState.Open)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "TGWatch closing", CancellationToken.None);
        }
        catch { }

        _socket?.Dispose();
        _socket = null;
        SetConnected(false);
    }
}
