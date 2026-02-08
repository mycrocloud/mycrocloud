using WebApp.Domain.Models;

namespace WebApp.Gateway.Middlewares.Api;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var route = (CachedRoute)context.Items["_CachedRoute"]!;
        var metadata = context.Items["_ApiRouteMetadata"] as ApiRouteMetadata;
        var authenticatedScheme = context.Items["_AuthenticatedScheme"] as CachedAuthenticationScheme;
        
        if (metadata == null || !metadata.RequireAuthorization)
        {
            await next.Invoke(context);
            return;
        }

        if (authenticatedScheme is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
        
        await next.Invoke(context);
    }
}
public static class AuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthorizationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationMiddleware>();
    }
}