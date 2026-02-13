using WebApp.Gateway.Models;
using WebApp.Gateway.Services;

namespace WebApp.Gateway.Middlewares.Api;

/// <summary>
/// Handler for static responses configured in the route.
/// Returns predefined status code, headers, and body content.
/// </summary>
public class StaticResponseHandler(IAppSpecificationService appCacheService, ILogger<StaticResponseHandler> logger) : IResponseHandler
{
    public ResponseType SupportedType => ResponseType.Static;

    public async Task HandleAsync(HttpContext context)
    {
        var route = (CachedRoute)context.Items["_CachedRoute"]!;
        var metadata = context.Items["_ApiRouteMetadata"] as ApiRouteMetadata;

        context.Response.StatusCode = metadata?.ResponseStatusCode ?? 200;

        foreach (var header in metadata?.ResponseHeaders ?? [])
        {
            context.Response.Headers.Append(header.Name, header.Value);
        }

        // Load response content from Deployment Manifest (indexed blobs)
        var app = (AppSpecification)context.Items["_AppSpecification"]!;
        var response = await appCacheService.GetApiDeploymentFileContentAsync(app.ApiDeploymentId, $"routes/{route.Id}/content");
        
        logger.LogDebug("Serving static response for route {RouteId}: StatusCode={StatusCode}, ContentLength={ContentLength}",
            route.Id, context.Response.StatusCode, response?.Length ?? 0);
        
        await context.Response.WriteAsync(response ?? string.Empty);
    }
}
