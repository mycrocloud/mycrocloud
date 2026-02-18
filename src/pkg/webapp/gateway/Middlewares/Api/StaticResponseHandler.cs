using MycroCloud.WebApp.Gateway.Models;
using MycroCloud.WebApp.Gateway.Services;

namespace MycroCloud.WebApp.Gateway.Middlewares.Api;

/// <summary>
/// Handler for static responses configured in the route.
/// Returns predefined status code, headers, and body content.
/// </summary>
public class StaticResponseHandler(IAppSpecificationService appCacheService, ILogger<StaticResponseHandler> logger) : IResponseHandler
{
    public ResponseType SupportedType => ResponseType.Static;

    public async Task HandleAsync(HttpContext context)
    {
        var route = (ApiRouteSummary)context.Items["_ApiRouteSummary"]!;
        var metadata = context.Items["_ApiRouteMetadata"] as ApiRouteMetadata;
        context.Response.StatusCode = metadata?.Response.StaticResponse?.StatusCode ?? 200;
        foreach (var header in metadata?.Response.StaticResponse?.Headers ?? [])
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
