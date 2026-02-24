using Npgsql;

namespace MycroCloud.WebApp.Gateway.Services;

public class CacheCleanupService(NpgsqlDataSource dataSource, ILogger<CacheCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await using var cmd = dataSource.CreateCommand("DELETE FROM cache_entries WHERE expires_at < now()");
                var deleted = await cmd.ExecuteNonQueryAsync(stoppingToken);
                if (deleted > 0)
                {
                    logger.LogDebug("Cleaned up {Count} expired cache entries", deleted);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error cleaning up expired cache entries");
            }
        }
    }
}
