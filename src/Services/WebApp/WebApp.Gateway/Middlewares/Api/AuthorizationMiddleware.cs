using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares.Api;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var route = (CachedRoute)context.Items["_CachedRoute"]!;
        var authenticatedScheme = context.Items["_AuthenticatedScheme"] as CachedAuthenticationScheme;
        if (!route.RequireAuthorization)
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