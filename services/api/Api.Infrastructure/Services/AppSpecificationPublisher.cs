using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Api.Domain.Enums;
using Api.Domain.Models;
using Api.Domain.Services;

namespace Api.Infrastructure.Services;

public class AppSpecificationPublisher(
    IDistributedCache cache,
    AppDbContext dbContext,
    ILogger<AppSpecificationPublisher> logger) : IAppSpecificationPublisher
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(30); // Optional pre-warming of versioned routes

    public async Task PublishAsync(string slug)
    {
        logger.LogInformation("Publishing spec for app: {Slug}", slug);

        var app = await dbContext.Apps
            .AsNoTracking()
            .Include(a => a.Routes.Where(r => r.Enabled && r.Status == RouteStatus.Active))
            .SingleOrDefaultAsync(a => a.Slug == slug);

        if (app is null)
        {
            logger.LogWarning("App {Slug} not found in DB, cannot publish spec", slug);
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT refresh_app_specification({app.Id})");

        // Pre-warm versioned routes cache
        if (app.ActiveApiDeploymentId.HasValue)
        {
            var routes = app.Routes.Select(MapToApiRouteSummary).ToList();
            var routesJson = JsonSerializer.Serialize(routes);
            var routesCacheKey = $"api_routes:{app.ActiveApiDeploymentId.Value}";
            await cache.SetStringAsync(routesCacheKey, routesJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            });
            logger.LogDebug("Pre-warmed route cache for deployment: {DeploymentId} ({RouteCount} routes)",
                app.ActiveApiDeploymentId.Value, routes.Count);
        }

        logger.LogInformation("Successfully published spec for app: {Slug} (SPA: {SpaId}, API: {ApiId})",
            slug, app.ActiveSpaDeploymentId, app.ActiveApiDeploymentId);
    }

    public async Task PublishByIdAsync(int appId)
    {
        var slug = await dbContext.Apps.Where(a => a.Id == appId)
            .Select(a => a.Slug).SingleOrDefaultAsync();
        if (slug != null) await PublishAsync(slug);
    }

    private static ApiRouteSummary MapToApiRouteSummary(Api.Domain.Entities.Route route) => new()
    {
        Id = route.Id,
        Method = route.Method,
        Path = route.Path,
        ResponseType = route.ResponseType
    };
}
