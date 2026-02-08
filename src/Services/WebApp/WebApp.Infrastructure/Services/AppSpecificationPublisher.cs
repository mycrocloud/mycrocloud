using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Models;
using WebApp.Domain.Services;

namespace WebApp.Infrastructure.Services;

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
            .Include(a => a.ActiveRelease)
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

        logger.LogInformation("Successfully published spec for app: {Slug} (Release: {ReleaseId})", 
            slug, spec.SpaDeploymentId);
    }

    public async Task InvalidateAsync(string slug)
    {
        var cacheKey = $"{CacheKeyPrefix}{slug}";
        await cache.RemoveAsync(cacheKey);
        logger.LogInformation("Invalidated spec for app: {Slug}", slug);
    }

    private static AppSpecification MapToSpecification(App app) => new()
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
