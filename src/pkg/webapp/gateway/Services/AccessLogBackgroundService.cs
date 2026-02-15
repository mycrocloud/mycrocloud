using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Services;

public class AccessLogBackgroundService(
    AccessLogChannel channel,
    IServiceScopeFactory scopeFactory,
    ILogger<AccessLogBackgroundService> logger) : BackgroundService
{
    private const int MaxBatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AccessLog>(MaxBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the first item
                var log = await channel.Reader.ReadAsync(stoppingToken);
                batch.Add(log);

                // Drain up to MaxBatchSize within the flush interval
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(FlushInterval);

                try
                {
                    while (batch.Count < MaxBatchSize)
                    {
                        log = await channel.Reader.ReadAsync(cts.Token);
                        batch.Add(log);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Flush interval elapsed or stopping — flush what we have
                }

                await FlushBatch(batch);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down — drain remaining items
                while (channel.Reader.TryRead(out var remaining))
                    batch.Add(remaining);

                if (batch.Count > 0)
                    await FlushBatch(batch);

                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing access log batch");
                batch.Clear();
            }
        }
    }

    private async Task FlushBatch(List<AccessLog> batch)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.Logs.AddRange(batch);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("Flushed {Count} access logs", batch.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to flush {Count} access logs", batch.Count);
        }
        finally
        {
            batch.Clear();
        }
    }
}
