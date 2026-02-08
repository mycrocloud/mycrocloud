using WebApp.Domain.Entities;
using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares.Api;

/// <summary>
/// Handler for static responses configured in the route.
/// Returns predefined status code, headers, and body content.
/// </summary>
public class StaticResponseHandler(IAppCacheService appCacheService, ILogger<StaticResponseHandler> logger) : IResponseHandler
{
    public ResponseType SupportedType => ResponseType.Static;

    public async Task HandleAsync(HttpContext context)
    {
        var route = (CachedRoute)context.Items["_CachedRoute"]!;

        context.Response.StatusCode = route.ResponseStatusCode ??
                                      throw new InvalidOperationException("ResponseStatusCode is null");

        foreach (var header in route.ResponseHeaders ?? [])
        {
            context.Response.Headers.Append(header.Name, header.Value);
        }

        // Load response content from DB (not cached)
        var response = await appCacheService.GetRouteResponseAsync(route.Id);
        
        logger.LogDebug("Serving static response for route {RouteId}: StatusCode={StatusCode}, ContentLength={ContentLength}",
            route.Id, context.Response.StatusCode, response?.Length ?? 0);
        
        await context.Response.WriteAsync(response ?? string.Empty);
    }
}
