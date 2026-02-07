using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares;

public class StaticResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppCacheService appCacheService)
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
        await context.Response.WriteAsync(response ?? string.Empty);
    }
}

public static class StaticResponseMiddlewareExtensions
{
    public static IApplicationBuilder UseStaticResponseMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticResponseMiddleware>();
    }
}