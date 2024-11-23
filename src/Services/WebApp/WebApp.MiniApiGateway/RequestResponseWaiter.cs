using WebApp.FunctionShared;

namespace WebApp.MiniApiGateway;

public class RequestResponseWaiter
{
    private readonly Dictionary<string, TaskCompletionSource<Result>> _waiters = new();

    private static readonly object Lock = new();

    public void Wait(string requestId, TaskCompletionSource<Result> tcs)
    {
        lock (Lock)
        {
            _waiters.TryAdd(requestId, tcs);
        }
    }

    public void Set(string requestId, Result result)
    {
        lock (Lock)
        {
            if (_waiters.TryGetValue(requestId, out var tcs))
            {
                tcs.SetResult(result);
            }
        }
    }
}