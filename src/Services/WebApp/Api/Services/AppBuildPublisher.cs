namespace Api.Services;

using System.Collections.Concurrent;
using System.Threading.Channels;

public interface IAppBuildPublisher
{
    IAsyncEnumerable<string> Subscribe(int appId, CancellationToken cancellationToken);
    void Publish(int appId, string message);
}

public class InMemoryAppBuildPublisher : IAppBuildPublisher
{
    private readonly ConcurrentDictionary<int, List<Channel<string>>> _subscribers = new();

    public IAsyncEnumerable<string> Subscribe(int appId, CancellationToken cancellationToken)
    {
        var channel = Channel.CreateUnbounded<string>();

        var list = _subscribers.GetOrAdd(appId, _ => []);
        lock (list)
        {
            list.Add(channel);
        }

        return ReadFromChannel();

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
    }

    public void Publish(int appId, string message)
    {
        if (!_subscribers.TryGetValue(appId, out var list))
            return;

        lock (list)
        {
            foreach (var channel in list.ToList())
            {
                channel.Writer.TryWrite(message);
            }
        }
    }
}
