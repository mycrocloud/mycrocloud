using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WebApp.Infrastructure;

namespace Api.Services;

public interface IAppCacheInvalidator
{
    Task InvalidateAsync(string appName);
    Task InvalidateByIdAsync(int appId);
}

public class AppCacheInvalidator(
    IDistributedCache cache,
    AppDbContext dbContext,
    ILogger<AppCacheInvalidator> logger) : IAppCacheInvalidator
{
    private const string CacheKeyPrefix = "app:";

    public async Task InvalidateAsync(string appName)
    {
        var cacheKey = $"{CacheKeyPrefix}{appName}";
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation("Invalidated Gateway cache for app: {AppName}", appName);
    }

    public async Task InvalidateByIdAsync(int appId)
    {
        var appName = await dbContext.Apps
            .Where(a => a.Id == appId)
            .Select(a => a.Name)
            .SingleOrDefaultAsync();

        if (appName is not null)
        {
            await InvalidateAsync(appName);
        }
        else
        {
            logger.LogWarning("App not found for cache invalidation. AppId: {AppId}", appId);
        }
    }
}
