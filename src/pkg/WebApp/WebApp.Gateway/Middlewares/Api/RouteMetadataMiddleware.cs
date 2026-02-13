using WebApp.Gateway.Models;
using WebApp.Gateway.Services;

namespace WebApp.Gateway.Middlewares.Api;

public class RouteMetadataMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppSpecificationService appSpecService)
    {
        var app = (AppSpecification)context.Items["_AppSpecification"]!;
        var route = (CachedRoute)context.Items["_CachedRoute"]!;

        if (app.ApiDeploymentId.HasValue)
        {
            var metadata = await appSpecService.GetRouteMetadataAsync(app.ApiDeploymentId.Value, route.Id);
            if (metadata != null)
            {
                context.Items["_ApiRouteMetadata"] = metadata;
            }
        }

        await next(context);
    }
}

public static class RouteMetadataMiddlewareExtensions
{
    public static IApplicationBuilder UseRouteMetadataMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RouteMetadataMiddleware>();
    }
}
