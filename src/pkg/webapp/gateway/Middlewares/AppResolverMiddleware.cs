using MycroCloud.WebApp.Gateway.Models;
using MycroCloud.WebApp.Gateway.Services;
using MycroCloud.WebApp.Gateway.Utils;

namespace MycroCloud.WebApp.Gateway.Middlewares;

public class AppResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppSpecificationService appCacheService, IConfiguration configuration)
    {
        AppSpecification? appSpec = null;

        var appName = context.Request.Evaluate(configuration["AppNameSource"] ?? "Header:X-App-Name");
        if (!string.IsNullOrEmpty(appName))
        {
            appSpec = await appCacheService.GetBySlugAsync(appName);
        }
        else
        {
            // Custom domain resolution — use the Host header directly
            var hostname = context.Request.Host.Host;
            appSpec = await appCacheService.GetByCustomDomainAsync(hostname);
        }

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