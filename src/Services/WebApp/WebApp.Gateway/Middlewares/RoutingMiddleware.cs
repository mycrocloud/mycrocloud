using System.Text.RegularExpressions;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Gateway.Cache;
using WebApp.Infrastructure;

namespace WebApp.Gateway.Middlewares;

public class RoutingMiddleware(RequestDelegate next, ILogger<RoutingMiddleware> logger)
{
    public async Task Invoke(HttpContext context, AppDbContext appDbContext)
    {
        var app = (CachedApp)context.Items["_CachedApp"]!;
        var isCustomConfig = app.RoutingConfig is { Routes.Count: > 0 };
        var config = isCustomConfig ? app.RoutingConfig : RoutingConfig.Default;

        var requestPath = context.Request.Path.Value ?? "/";

        logger.LogDebug("Routing request: Path={Path}, App={AppName}, UsingCustomConfig={IsCustom}, RouteCount={RouteCount}",
            requestPath, app.Name, isCustomConfig, config!.Routes.Count);

        // Find matching route by priority (lower priority number = higher precedence)
        var matchedRoute = config.Routes
            .OrderBy(r => r.Priority ?? int.MaxValue)
            .FirstOrDefault(r => MatchRoute(requestPath, r.Match));

        if (matchedRoute is null)
        {
            logger.LogDebug("No route matched for path: {Path}", requestPath);
            context.Response.StatusCode = 404;
            return;
        }

        logger.LogDebug("Route matched: Name={RouteName}, MatchType={MatchType}, MatchPath={MatchPath}, TargetType={TargetType}",
            matchedRoute.Name ?? "(unnamed)", matchedRoute.Match.Type, matchedRoute.Match.Path, matchedRoute.Target.Type);

        // Store the matched routing config route for downstream middlewares
        context.Items["_RoutingConfigRoute"] = matchedRoute;

        switch (matchedRoute.Target.Type)
        {
            case RouteTargetType.Api:
                // Apply strip prefix if configured
                if (matchedRoute.Target.StripPrefix == true)
                {
                    var strippedPath = StripPrefix(requestPath, matchedRoute.Match.Path);
                    logger.LogDebug("Stripped prefix: {OriginalPath} -> {StrippedPath}", requestPath, strippedPath);
                    context.Request.Path = strippedPath;
                }
                // Continue to RouteResolverMiddleware
                await next(context);
                break;

            case RouteTargetType.Static:
                await HandleStaticRequest(context, app, appDbContext, requestPath, matchedRoute, logger);
                break;

            default:
                logger.LogWarning("Unknown target type: {TargetType}", matchedRoute.Target.Type);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Unknown target type");
                break;
        }
    }

    private static bool MatchRoute(string requestPath, RouteMatch match)
    {
        return match.Type switch
        {
            RouteMatchType.Prefix => requestPath.StartsWith(match.Path, StringComparison.OrdinalIgnoreCase),
            RouteMatchType.Exact => requestPath.Equals(match.Path, StringComparison.OrdinalIgnoreCase),
            RouteMatchType.Regex => Regex.IsMatch(requestPath, match.Path, RegexOptions.IgnoreCase),
            _ => false
        };
    }

    private static string StripPrefix(string requestPath, string prefix)
    {
        if (requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var stripped = requestPath[prefix.Length..];
            return string.IsNullOrEmpty(stripped) ? "/" : stripped;
        }
        return requestPath;
    }

    private static async Task HandleStaticRequest(HttpContext context, CachedApp app, AppDbContext appDbContext,
        string requestPath, RoutingConfigRoute route, ILogger logger)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            logger.LogDebug("Static request rejected: Method {Method} not allowed", context.Request.Method);
            context.Response.StatusCode = 405;
            return;
        }

        if (app.LatestBuildId is null)
        {
            logger.LogDebug("Static request failed: No build available for app {AppName}", app.Name);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("No build available");
            return;
        }

        var filePath = GetFilePath(requestPath, route);
        logger.LogDebug("Static request: RequestPath={RequestPath}, ResolvedFilePath={FilePath}", requestPath, filePath ?? "(null)");

        if (filePath is not null)
        {
            var file = await FindBuildArtifact(appDbContext, app.LatestBuildId.Value, filePath);
            if (file is not null)
            {
                logger.LogDebug("Serving static file: {FilePath}", filePath);
                await ServeFile(context, file);
                return;
            }
            logger.LogDebug("File not found: {FilePath}", filePath);
        }

        // Fallback (for SPA client-side routing, or root path "/")
        if (!string.IsNullOrEmpty(route.Target.Fallback))
        {
            var fallbackPath = route.Target.Fallback.TrimStart('/');
            logger.LogDebug("Trying fallback: {FallbackPath}", fallbackPath);
            var fallbackFile = await FindBuildArtifact(appDbContext, app.LatestBuildId.Value, fallbackPath);

            if (fallbackFile is not null)
            {
                logger.LogDebug("Serving fallback file: {FallbackPath}", fallbackPath);
                await ServeFile(context, fallbackFile);
                return;
            }
            logger.LogDebug("Fallback file not found: {FallbackPath}", fallbackPath);
        }

        logger.LogDebug("Static request: No file found, returning 404");
        context.Response.StatusCode = 404;
    }

    private static string? GetFilePath(string requestPath, RoutingConfigRoute route)
    {
        var filePath = requestPath;

        // Apply strip prefix if configured
        if (route.Target.StripPrefix == true)
        {
            filePath = StripPrefix(filePath, route.Match.Path);
        }

        // Apply rewrite if configured
        if (!string.IsNullOrEmpty(route.Target.Rewrite))
        {
            filePath = route.Target.Rewrite;
        }

        // No file path (root request) - let fallback handle it
        if (string.IsNullOrEmpty(filePath) || filePath == "/")
        {
            return null;
        }

        return filePath.TrimStart('/');
    }

    private static async Task<AppBuildArtifact?> FindBuildArtifact(AppDbContext appDbContext, Guid buildId, string path)
    {
        return await appDbContext.AppBuildArtifacts
            .SingleOrDefaultAsync(f => f.BuildId == buildId && f.Path == path);
    }

    private static async Task ServeFile(HttpContext context, AppBuildArtifact file)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(file.Path, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        context.Response.ContentType = contentType;
        await context.Response.Body.WriteAsync(file.Content);
        await context.Response.CompleteAsync();
    }
}

public static class RoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseRoutingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoutingMiddleware>();
    }
}
