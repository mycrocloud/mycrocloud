using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Infrastructure;
using WebApp.Domain.Models;
using WebApp.Domain.Services;

namespace WebApp.Gateway.Cache;

public interface IAppSpecificationService
{
    Task<AppSpecification?> GetBySlugAsync(string slug);
    Task InvalidateAsync(string slug);

    /// <summary>
    /// Get content from an API deployment manifest.
    /// </summary>
    Task<string?> GetApiDeploymentFileContentAsync(Guid? deploymentId, string path);

    Task<ApiRouteMetadata?> GetRouteMetadataAsync(Guid? deploymentId, int routeId);
}

public class AppSpecificationService(
    IDistributedCache cache,
    AppDbContext dbContext,
    IStorageProvider storageProvider,
    ILogger<AppSpecificationService> logger) : IAppSpecificationService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "app:";
    private const string MetaCacheKeyPrefix = "route_meta:";

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

    public async Task<string?> GetApiDeploymentFileContentAsync(Guid? deploymentId, string path)
    {
        if (deploymentId == null) return null;

        var file = await dbContext.DeploymentFiles
            .AsNoTracking()
            .Include(f => f.Blob)
            .FirstOrDefaultAsync(f => f.DeploymentId == deploymentId && f.Path == path);

        if (file == null) return null;

        using var stream = await storageProvider.OpenReadAsync(file.Blob.StorageKey);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    public async Task<ApiRouteMetadata?> GetRouteMetadataAsync(Guid? deploymentId, int routeId)
    {
        if (deploymentId == null) return null;

        var cacheKey = $"{MetaCacheKeyPrefix}{deploymentId}:{routeId}";
        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<ApiRouteMetadata>(cached);
        }

        var json = await GetApiDeploymentFileContentAsync(deploymentId, $"routes/{routeId}/meta.json");
        if (json == null) return null;

        var meta = JsonSerializer.Deserialize<ApiRouteMetadata>(json);
        if (meta != null)
        {
            await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // Long TTL for versioned metadata
            });
        }

        return meta;
    }
}
