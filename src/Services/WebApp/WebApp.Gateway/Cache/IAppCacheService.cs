using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Infrastructure;

namespace WebApp.Gateway.Cache;

public interface IAppCacheService
{
    Task<CachedApp?> GetByNameAsync(string appName);
    Task InvalidateAsync(string appName);

    /// <summary>
    /// Get function code for a route. Not cached - loaded directly from DB.
    /// </summary>
    Task<string?> GetRouteResponseAsync(int routeId);
}

public class AppCacheService(
    IDistributedCache cache,
    AppDbContext dbContext,
    ILogger<AppCacheService> logger) : IAppCacheService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "app:";

    public async Task<CachedApp?> GetByNameAsync(string appName)
    {
        var cacheKey = $"{CacheKeyPrefix}{appName}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
        {
            logger.LogDebug("Cache hit for app: {AppName}", appName);
            return JsonSerializer.Deserialize<CachedApp>(cached);
        }

        logger.LogDebug("Cache miss for app: {AppName}, loading from DB", appName);

        var app = await dbContext.Apps
            .AsNoTracking()
            .Include(a => a.Routes.Where(r => r.Enabled && r.Status == RouteStatus.Active))
            .Include(a => a.AuthenticationSchemes.Where(s => s.Enabled))
            .Include(a => a.Variables.Where(v => v.Target == VariableTarget.Runtime || v.Target == VariableTarget.All))
            .Include(a => a.ActiveRelease)
                .ThenInclude(r => r!.SpaDeployment)
            .SingleOrDefaultAsync(a => a.Slug == appName);

        if (app is null)
            return null;

        var cachedApp = MapToCachedApp(app);

        var json = JsonSerializer.Serialize(cachedApp);
        await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        });

        logger.LogDebug("Cached app: {AppName}", appName);

        return cachedApp;
    }

    public async Task InvalidateAsync(string appName)
    {
        var cacheKey = $"{CacheKeyPrefix}{appName}";
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation("Invalidated cache for app: {AppName}", appName);
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

    private static CachedApp MapToCachedApp(App app) => new()
    {
        Id = app.Id,
        Slug = app.Slug,
        OwnerId = app.OwnerId,
        State = app.State,
        SpaDeploymentId = app.ActiveRelease?.SpaDeploymentId,
        ApiCorsSettings = app.CorsSettings ?? CorsSettings.Default,
        RoutingConfig = app.RoutingConfig ?? RoutingConfig.Default,
        Settings = app.Settings ?? AppSettings.Default,
        Routes = app.Routes.Select(MapToCachedRoute).ToList(),
        AuthenticationSchemes = app.AuthenticationSchemes.Select(MapToCachedAuthScheme).ToList(),
        Variables = app.Variables.Select(MapToCachedVariable).ToList()
    };

    private static CachedRoute MapToCachedRoute(Route route) => new()
    {
        Id = route.Id,
        Method = route.Method,
        Path = route.Path,
        ResponseType = route.ResponseType,
        ResponseStatusCode = route.ResponseStatusCode,
        ResponseHeaders = route.ResponseHeaders ?? [],
        RequestQuerySchema = route.RequestQuerySchema,
        RequestHeaderSchema = route.RequestHeaderSchema,
        RequestBodySchema = route.RequestBodySchema,
        RequireAuthorization = route.RequireAuthorization,
        FunctionRuntime = route.FunctionRuntime
    };

    private static CachedAuthenticationScheme MapToCachedAuthScheme(AuthenticationScheme scheme) => new()
    {
        Type = scheme.Type,
        OpenIdConnectAuthority = scheme.OpenIdConnectAuthority,
        OpenIdConnectAudience = scheme.OpenIdConnectAudience,
    };

    private static CachedVariable MapToCachedVariable(Variable variable) => new()
    {
        Name = variable.Name,
        Value = variable.Value,
    };
}
