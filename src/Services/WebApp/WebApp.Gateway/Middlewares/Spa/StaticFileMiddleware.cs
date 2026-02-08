using Microsoft.AspNetCore.StaticFiles;
using WebApp.Domain.Entities;
using WebApp.Gateway.Cache;
using WebApp.Infrastructure;

namespace WebApp.Gateway.Middlewares.Spa;

public class StaticFileMiddleware(RequestDelegate next, ILogger<StaticFileMiddleware> logger)
{
    public async Task Invoke(HttpContext context, AppDbContext appDbContext)
    {
        var app = (CachedApp)context.Items["_CachedApp"]!;
        var route = (RoutingConfigRoute)context.Items["_RoutingConfigRoute"]!;
        var requestPath = context.Request.Path.Value ?? "/";

        await HandleStaticRequest(context, app, appDbContext, requestPath, route, logger);
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

        // Check if we have any deployment available
        if (app.SpaDeploymentId is null && app.SpaExtractedPath is null)
        {
            logger.LogDebug("Static request failed: No build available for app {AppName}", app.Slug);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("No build available");
            return;
        }

        var filePath = GetFilePath(requestPath, route);
        logger.LogDebug("Static request: RequestPath={RequestPath}, ResolvedFilePath={FilePath}", requestPath, filePath ?? "(null)");

        if (!string.IsNullOrEmpty(app.SpaExtractedPath))
        {
            if (filePath is not null)
            {
                var diskPath = Path.Combine(app.SpaExtractedPath, filePath);
                if (File.Exists(diskPath))
                {
                    logger.LogDebug("Serving static file from disk: {FilePath}", filePath);
                    await ServeFileFromDisk(context, diskPath);
                    return;
                }
                logger.LogDebug("File not found on disk: {FilePath}", filePath);
            }

            // Fallback for SPA (from disk)
            if (!string.IsNullOrEmpty(route.Target.Fallback))
            {
                var fallbackPath = route.Target.Fallback.TrimStart('/');
                var diskFallbackPath = Path.Combine(app.SpaExtractedPath, fallbackPath);
                if (File.Exists(diskFallbackPath))
                {
                    logger.LogDebug("Serving fallback file from disk: {FallbackPath}", fallbackPath);
                    await ServeFileFromDisk(context, diskFallbackPath);
                    return;
                }
                logger.LogDebug("Fallback file not found on disk: {FallbackPath}", fallbackPath);
            }
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

    private static string StripPrefix(string requestPath, string prefix)
    {
        if (requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var stripped = requestPath[prefix.Length..];
            return string.IsNullOrEmpty(stripped) ? "/" : stripped;
        }
        return requestPath;
    }

    private static async Task ServeFileFromDisk(HttpContext context, string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        context.Response.ContentType = contentType;
        await context.Response.SendFileAsync(filePath);
    }
}

public static class StaticFileMiddlewareExtensions
{
    public static IApplicationBuilder UseSpaStaticFileMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticFileMiddleware>();
    }
}
