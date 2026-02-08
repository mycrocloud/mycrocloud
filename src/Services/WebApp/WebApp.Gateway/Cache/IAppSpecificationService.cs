using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Infrastructure;
using WebApp.Domain.Models;

namespace WebApp.Gateway.Cache;

public interface IAppSpecificationService
{
    Task<AppSpecification?> GetBySlugAsync(string slug);
    Task InvalidateAsync(string slug);

    /// <summary>
    /// Get function code for a route. Not cached - loaded directly from DB.
    /// </summary>
    Task<string?> GetRouteResponseAsync(int routeId);
}

public class AppSpecificationService(
    IDistributedCache cache,
    AppDbContext dbContext,
    ILogger<AppSpecificationService> logger) : IAppSpecificationService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "app:";

    public async Task<AppSpecification?> GetBySlugAsync(string slug)
    {
        var cacheKey = $"{CacheKeyPrefix}{slug}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for spec: {Slug}", slug);
            return JsonSerializer.Deserialize<AppSpecification>(cached);
        }

        logger.LogWarning("Cache miss for spec: {Slug}. Spec must be published from API.", slug);
        return null;
    }

    public async Task InvalidateAsync(string slug)
    {
        var cacheKey = $"{CacheKeyPrefix}{slug}";
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation("Invalidated spec for app: {Slug}", slug);
    }

    public async Task<string?> GetRouteResponseAsync(int routeId)
    {
        var route = await dbContext.Routes
            .AsNoTracking()
            .Where(r => r.Id == routeId)
            .Select(r => r.Response)
            .SingleOrDefaultAsync();

        return route;
    }
}
