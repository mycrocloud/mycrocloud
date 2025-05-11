using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;

namespace WebApp.ApiGateway.Middlewares;

public class AppResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppRepository appRepository, IConfiguration configuration)
    {
        int? appId = null;
        var source = configuration["AppIdSource"]!.Split(":")[0];
        var name = configuration["AppIdSource"]!.Split(":")[1];
        if (source.Equals("Header", StringComparison.OrdinalIgnoreCase) && context.Request.Headers.TryGetValue(name, out var headerAppId)
            && int.TryParse(headerAppId, out var parsedAppId))
        {
            appId = parsedAppId;
        }

        if (appId is null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("App not found");
            return;
        }
        var app = await appRepository.FindByAppId(appId.Value);
        if (app is null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("App not found");
            return;
        }

        switch (app.Status)
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
        context.Items["_App"] = app;
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