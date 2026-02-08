using WebApp.Domain.Enums;
using WebApp.Domain.Models;
using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares;

public class AppResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppSpecificationService appCacheService, IConfiguration configuration)
    {
        var appName = context.Request.Evaluate(configuration["AppNameSource"] ?? "Header:X-App-Name");

        if (string.IsNullOrEmpty(appName))
        {
            await context.Response.WriteNotFound("App not found");
            return;
        }

        var appSpec = await appCacheService.GetBySlugAsync(appName);
        if (appSpec is null)
        {
            await context.Response.WriteNotFound("App not found");
            return;
        }

        switch (appSpec.State)
        {
            case AppState.Disabled:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("App is disabled");
                return;
            case AppState.Deleted:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("App is deleted");
                return;
        }

        context.Items["_AppSpecification"] = appSpec;
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