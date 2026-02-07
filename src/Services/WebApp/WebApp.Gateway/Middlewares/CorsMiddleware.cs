using Microsoft.AspNetCore.Cors.Infrastructure;
using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares;

public class CorsMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<CorsMiddleware> logger)
    {
        var app = (CachedApp)context.Items["_CachedApp"]!;
        var requestHeaders = context.Request.Headers;
        if (context.Request.IsPreflightRequest())
        {
            context.Response.Headers.Append(CorsConstants.AccessControlAllowOrigin,
                            string.Join(", ", app.CorsSettings.AllowedOrigins ?? []));
            context.Response.Headers.Append(CorsConstants.AccessControlAllowMethods,
                            string.Join(", ", app.CorsSettings.AllowedMethods ?? []));
            context.Response.Headers.Append(CorsConstants.AccessControlAllowHeaders,
                             string.Join(", ", app.CorsSettings.AllowedHeaders ?? []));

            if (app.CorsSettings.ExposeHeaders != null)
            {
                context.Response.Headers.Append(CorsConstants.AccessControlExposeHeaders,
                            string.Join(", ", app.CorsSettings.ExposeHeaders));
            }
            if (app.CorsSettings.MaxAgeSeconds != null)
            {
                context.Response.Headers.Append(CorsConstants.AccessControlMaxAge,
                            app.CorsSettings.MaxAgeSeconds.ToString());
            }

            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        context.Response.Headers.Append(CorsConstants.AccessControlAllowOrigin, requestHeaders.Origin);
        context.Response.Headers.Append(CorsConstants.AccessControlAllowMethods,
                            requestHeaders.AccessControlRequestMethod);
        context.Response.Headers.Append(CorsConstants.AccessControlAllowHeaders,
                            requestHeaders.AccessControlRequestHeaders);

        await next(context);
    }
}

public static class CorsMiddlewareExtensions
{
    public static IApplicationBuilder UseCorsMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorsMiddleware>();
    }
}