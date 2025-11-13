namespace WebApp.Api.Services;

using System.Collections.Concurrent;
using System.Threading.Channels;

public interface IAppBuildPublisher
{
    IAsyncEnumerable<string> Subscribe(int appId, CancellationToken cancellationToken);
    void Publish(int appId);
}

public class AppBuildPublisher : IAppBuildPublisher
{
    // appId → list of subscribers (many SSE connections)
    private readonly ConcurrentDictionary<int, List<Channel<string>>> _subscribers 
        = new();

    public IAsyncEnumerable<string> Subscribe(int appId, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<string>();

        // Add subscriber
        var list = _subscribers.GetOrAdd(appId, _ => new List<Channel<string>>());
        lock (list)
        {
            list.Add(channel);
        }

        // Remove subscriber when SSE closes
        async IAsyncEnumerable<string> ReadFromChannel()
        {
            try
            {
                await foreach (var msg in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    yield return msg;
                }
            }
            finally
            {
                lock (list)
                {
                    list.Remove(channel);
                }
            }
        }

        return ReadFromChannel();
    }

    public void Publish(int appId)
    {
        if (!_subscribers.TryGetValue(appId, out var list))
            return;

        lock (list)
        {
            foreach (var channel in list.ToList())
            {
                channel.Writer.TryWrite("update");
            }
        }
    }
}
