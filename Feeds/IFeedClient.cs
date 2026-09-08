using TGWatch.Windows.Core;

namespace TGWatch.Windows.Feeds;

public interface IFeedClient : IAsyncDisposable
{
    string Name { get; }
    bool IsConnected { get; }

    event Action<FeedActivityEvent>? Activity;
    event Action<string, bool>? ConnectionChanged;

    Task StartAsync(CancellationToken cancellationToken);
}
