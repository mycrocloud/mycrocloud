using WebApp.Domain.Entities;

namespace WebApp.Gateway.Middlewares;

public class AuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var route = (Route)context.Items["_Route"]!;
        var authenticatedScheme = context.Items["_AuthenticatedScheme"] as AuthenticationScheme;
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