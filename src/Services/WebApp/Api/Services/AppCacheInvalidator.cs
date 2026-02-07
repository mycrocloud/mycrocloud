using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WebApp.Infrastructure;

namespace Api.Services;

public interface IAppCacheInvalidator
{
    Task InvalidateAsync(string slug);
    Task InvalidateByIdAsync(int appId);
}

public class AppCacheInvalidator(
    IDistributedCache cache,
    AppDbContext dbContext,
    ILogger<AppCacheInvalidator> logger) : IAppCacheInvalidator
{
    private const string CacheKeyPrefix = "app:";

    public async Task InvalidateAsync(string slug)
    {
        var cacheKey = $"{CacheKeyPrefix}{slug}";
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation("Invalidated Gateway cache for app: {AppSlug}", slug);
    }

    public async Task InvalidateByIdAsync(int appId)
    {
        var slug = await dbContext.Apps
            .Where(a => a.Id == appId)
            .Select(a => a.Slug)
            .SingleOrDefaultAsync();
        
        if (slug is not null)
        {
            await InvalidateAsync(slug);
        }
        else
        {
            logger.LogWarning("App not found for cache invalidation. AppId: {AppId}", appId);
        }
    }
}
