using System.Collections.Concurrent;

namespace MycroCloud.WebApp.Gateway.Services;

public class ConcurrentJobQueue : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _jobQueue = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _processingTask;

    public ConcurrentJobQueue(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency);
        _processingTask = Task.Run(ProcessQueueAsync);
    }

    public Task<TResult> EnqueueAsync<TResult>(Func<CancellationToken, Task<TResult>> job, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<TResult>();
        _jobQueue.Enqueue(async (cancellationToken) =>
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                var result = await job(linkedCts.Token);
                tcs.TrySetResult(result);
            }
            catch (OperationCanceledException)
            {
                tcs.TrySetCanceled();
                Console.WriteLine("Job timed out or was canceled.");
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
                Console.WriteLine($"Job failed with exception: {ex.Message}");
            }
        });

        return tcs.Task;
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            while (!_shutdownCts.Token.IsCancellationRequested)
            {
                while (_jobQueue.TryDequeue(out var job))
                {
                    await _semaphore.WaitAsync(_shutdownCts.Token);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await job(_shutdownCts.Token);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                }

                await Task.Delay(100); // Optional delay to prevent tight loop
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
    }

    public void Dispose()
    {
        _shutdownCts.Cancel();
        _processingTask.Wait();
        _semaphore.Dispose();
        _shutdownCts.Dispose();
    }
}
