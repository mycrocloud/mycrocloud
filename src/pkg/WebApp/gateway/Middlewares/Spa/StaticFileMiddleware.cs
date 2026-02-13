using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using MycroCloud.WebApp.Gateway.Models;
using MycroCloud.WebApp.Gateway.Services;

namespace MycroCloud.WebApp.Gateway.Middlewares.Spa;

public class StaticFileMiddleware(RequestDelegate next, ILogger<StaticFileMiddleware> logger)
{
    public async Task Invoke(HttpContext context, AppDbContext appDbContext, IStorageProvider storageProvider)
    {
        var app = (AppSpecification)context.Items["_AppSpecification"]!;
        var route = (RoutingConfigRoute)context.Items["_RoutingConfigRoute"]!;
        var requestPath = context.Request.Path.Value ?? "/";

        await HandleStaticRequest(context, app, appDbContext, storageProvider, requestPath, route, logger);
    }

    private static async Task HandleStaticRequest(HttpContext context, AppSpecification app, AppDbContext appDbContext,
        IStorageProvider storageProvider, string requestPath, RoutingConfigRoute route, ILogger logger)
    {
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            logger.LogDebug("Static request rejected: Method {Method} not allowed", context.Request.Method);
            context.Response.StatusCode = 405;
            return;
        }

        // Check if we have any deployment available
        if (app.SpaDeploymentId is null)
        {
            logger.LogDebug("Static request failed: No deployment available for app {AppName}", app.Slug);
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("No deployment available");
            return;
        }

        var filePath = GetFilePath(requestPath, route);
        logger.LogDebug("Static request: RequestPath={RequestPath}, ResolvedFilePath={FilePath}", requestPath, filePath ?? "(null)");

        // 1. Try resolving file from manifest
        if (filePath is not null)
        {
            var deploymentFile = await appDbContext.DeploymentFiles
                .AsNoTracking()
                .Include(f => f.Blob)
                .FirstOrDefaultAsync(f => f.DeploymentId == app.SpaDeploymentId && f.Path == filePath);

            if (deploymentFile != null)
            {
                logger.LogDebug("Serving static file from manifest: {FilePath} (Blob: {BlobKey})", filePath, deploymentFile.Blob.StorageKey);
                await ServeFileFromStorage(context, storageProvider, deploymentFile.Blob.StorageKey, filePath, deploymentFile.ETag);
                return;
            }
            logger.LogDebug("File not found in manifest: {FilePath}", filePath);
        }

        // 2. Fallback for SPA (from manifest)
        if (!string.IsNullOrEmpty(route.Target.Fallback))
        {
            var fallbackPath = route.Target.Fallback.TrimStart('/');
            var deploymentFile = await appDbContext.DeploymentFiles
                .AsNoTracking()
                .Include(f => f.Blob)
                .FirstOrDefaultAsync(f => f.DeploymentId == app.SpaDeploymentId && f.Path == fallbackPath);

            if (deploymentFile != null)
            {
                logger.LogDebug("Serving fallback file from manifest: {FallbackPath} (Blob: {BlobKey})", fallbackPath, deploymentFile.Blob.StorageKey);
                await ServeFileFromStorage(context, storageProvider, deploymentFile.Blob.StorageKey, fallbackPath, deploymentFile.ETag);
                return;
            }
            logger.LogDebug("Fallback file not found in manifest: {FallbackPath}", fallbackPath);
        }

        logger.LogDebug("Static request: No file found, returning 404");
        context.Response.StatusCode = 404;
    }

    private static async Task ServeFileFromStorage(HttpContext context, IStorageProvider storageProvider, string storageKey, string fileName, string etag)
    {
        // ETag check
        var requestHeaders = context.Request.Headers;
        if (requestHeaders.TryGetValue(HeaderNames.IfNoneMatch, out var ifNoneMatch) && ifNoneMatch == $"\"{etag}\"")
        {
            context.Response.StatusCode = 304;
            return;
        }

        context.Response.Headers[HeaderNames.ETag] = $"\"{etag}\"";
        context.Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000"; // Long cache since it's immutable

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        context.Response.ContentType = contentType;
        
        using var stream = await storageProvider.OpenReadAsync(storageKey);
        await stream.CopyToAsync(context.Response.Body);
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

}

public static class StaticFileMiddlewareExtensions
{
    public static IApplicationBuilder UseSpaStaticFileMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticFileMiddleware>();
    }
}
