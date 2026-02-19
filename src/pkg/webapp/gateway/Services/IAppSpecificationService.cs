using System.Text.Json;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Services;

public interface IAppSpecificationService
{
    Task<AppSpecification?> GetBySlugAsync(string slug);

    Task<AppSpecification?> GetByCustomDomainAsync(string hostname);
    
    Task<string?> GetApiDeploymentFileContentAsync(Guid? deploymentId, string path);

    Task<ApiRouteMetadata?> GetRouteMetadataAsync(Guid? deploymentId, int routeId);

    Task<List<ApiRouteSummary>> GetApiRoutesAsync(Guid? deploymentId);
}

public class AppSpecificationService(
    IDistributedCache cache,
    Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache,
    AppDbContext dbContext,
    IStorageProvider storageProvider,
    ILogger<AppSpecificationService> logger) : IAppSpecificationService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "app:";
    private const string MetaCacheKeyPrefix = "route_meta:";
    private const string DomainIndexPrefix = "custom_domain:";

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

    public async Task<AppSpecification?> GetByCustomDomainAsync(string hostname)
    {
        var indexKey = $"{DomainIndexPrefix}{hostname.ToLowerInvariant()}";
        var slug = await cache.GetStringAsync(indexKey);
        if (slug is null)
        {
            logger.LogDebug("No custom domain index entry for: {Hostname}", hostname);
            return null;
        }
        return await GetBySlugAsync(slug);
    }

    public async Task<string?> GetApiDeploymentFileContentAsync(Guid? deploymentId, string path)
    {
        if (deploymentId == null) return null;
        var normalizedPath = path.TrimStart('/');

        var file = await dbContext.DeploymentFiles
            .AsNoTracking()
            .Include(f => f.Blob)
            .FirstOrDefaultAsync(f =>
                f.DeploymentId == deploymentId &&
                (f.Path == path || f.Path == normalizedPath));

        if (file == null) return null;

        var retryDelays = new[] { 100, 300, 600 };
        for (var attempt = 0; attempt < retryDelays.Length; attempt++)
        {
            try
            {
                using var stream = await storageProvider.OpenReadAsync(file.Blob.StorageKey);
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
                                               string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
            {
                if (attempt == retryDelays.Length - 1) break;

                logger.LogWarning(ex,
                    "Storage key not found yet. Retrying read for DeploymentId={DeploymentId}, Path={Path}, StorageKey={StorageKey}, Attempt={Attempt}",
                    deploymentId, path, file.Blob.StorageKey, attempt + 1);
                await Task.Delay(retryDelays[attempt]);
            }
            catch (FileNotFoundException ex)
            {
                if (attempt == retryDelays.Length - 1) break;

                logger.LogWarning(ex,
                    "Storage file not found yet. Retrying read for DeploymentId={DeploymentId}, Path={Path}, StorageKey={StorageKey}, Attempt={Attempt}",
                    deploymentId, path, file.Blob.StorageKey, attempt + 1);
                await Task.Delay(retryDelays[attempt]);
            }
        }

        logger.LogError(
            "Failed to load deployment file content after retries. DeploymentId={DeploymentId}, Path={Path}, StorageKey={StorageKey}",
            deploymentId, path, file.Blob.StorageKey);
        return null;
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

    public async Task<List<ApiRouteSummary>> GetApiRoutesAsync(Guid? deploymentId)
    {
        if (deploymentId == null) return [];

        var cacheKey = $"api_routes:{deploymentId}";

        // L1: Memory Cache
        if (memoryCache.TryGetValue(cacheKey, out List<ApiRouteSummary>? routes) && routes != null)
        {
            return routes;
        }

        // L2: Redis Cache
        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            routes = JsonSerializer.Deserialize<List<ApiRouteSummary>>(cached);
            if (routes != null)
            {
                memoryCache.Set(cacheKey, routes, CacheTtl);
                return routes;
            }
        }

        // Fallback: Storage (routes.json)
        logger.LogWarning("Cache miss for routes: {DeploymentId}. Loading from storage.", deploymentId);
        var json = await GetApiDeploymentFileContentAsync(deploymentId, "routes.json");
        if (json == null) return [];

        try
        {
            routes = JsonSerializer.Deserialize<List<ApiRouteSummary>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (routes != null)
            {
                await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheTtl
                });
                memoryCache.Set(cacheKey, routes, CacheTtl);
                return routes;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse routes.json for deployment {DeploymentId}", deploymentId);
        }

        return [];
    }
}
