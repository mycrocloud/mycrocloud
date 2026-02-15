using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Models;
using Api.Domain.Services;

namespace Api.Infrastructure.Services;

public class AppSpecificationPublisher(
    IDistributedCache cache,
    AppDbContext dbContext,
    ILogger<AppSpecificationPublisher> logger) : IAppSpecificationPublisher
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30); // Long TTL for spec, invalidated explicitly
    private const string CacheKeyPrefix = "app:";

    public async Task PublishAsync(string slug)
    {
        logger.LogInformation("Publishing spec for app: {Slug}", slug);

        var app = await dbContext.Apps
            .AsNoTracking()
            .Include(a => a.Routes.Where(r => r.Enabled && r.Status == RouteStatus.Active))
            .Include(a => a.AuthenticationSchemes.Where(s => s.Enabled))
            .Include(a => a.Variables.Where(v => v.Target == VariableTarget.Runtime || v.Target == VariableTarget.All))
            .SingleOrDefaultAsync(a => a.Slug == slug);

        if (app is null)
        {
            logger.LogWarning("App {Slug} not found in DB, cannot publish spec", slug);
            return;
        }

        var spec = MapToSpecification(app);
        var json = JsonSerializer.Serialize(spec);
        var cacheKey = $"{CacheKeyPrefix}{slug}";

        await cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        });

        logger.LogInformation("Successfully published spec for app: {Slug} (SPA: {SpaId}, API: {ApiId})", 
            slug, spec.SpaDeploymentId, spec.ApiDeploymentId);
    }

    public async Task InvalidateAsync(string slug)
    {
        var cacheKey = $"{CacheKeyPrefix}{slug}";
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation("Invalidated spec for app: {Slug}", slug);
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

    private static AppSpecification MapToSpecification(App app) => new()
    {
        Id = app.Id,
        Slug = app.Slug,
        OwnerId = app.OwnerId,
        State = app.State,
        SpaDeploymentId = app.ActiveSpaDeploymentId,
        ApiDeploymentId = app.ActiveApiDeploymentId,
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
        ResponseType = route.ResponseType
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
