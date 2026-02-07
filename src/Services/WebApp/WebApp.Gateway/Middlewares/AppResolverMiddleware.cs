using WebApp.Domain.Enums;
using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares;

public class AppResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppCacheService appCacheService, IConfiguration configuration)
    {
        var appName = context.Request.Evaluate(configuration["AppNameSource"] ?? "Header:X-App-Name");

        if (string.IsNullOrEmpty(appName))
        {
            await context.Response.WriteNotFound("App not found");
            return;
        }

        var cachedApp = await appCacheService.GetByNameAsync(appName);
        if (cachedApp is null)
        {
            await context.Response.WriteNotFound("App not found");
            return;
        }

        switch (cachedApp.Status)
        {
            case AppStatus.Inactive:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("App is inactive");
                return;
            case AppStatus.Blocked:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("App is blocked");
                return;
        }

        context.Items["_CachedApp"] = cachedApp;
        await next(context);
    }
}

public static class AppResolverMiddlewareExtensions
{
    public static IApplicationBuilder UseAppResolverMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AppResolverMiddleware>();
    }
}