using Microsoft.AspNetCore.Cors.Infrastructure;
using MycroCloud.WebApp.Gateway.Models;
using MycroCloud.WebApp.Gateway.Utils;

namespace MycroCloud.WebApp.Gateway.Middlewares.Api;

public class CorsMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<CorsMiddleware> logger)
    {
        var app = (AppSpecification)context.Items["_AppSpecification"]!;
        var requestHeaders = context.Request.Headers;
        if (context.Request.IsPreflightRequest())
        {
            context.Response.Headers.Append(CorsConstants.AccessControlAllowOrigin,
                            string.Join(", ", app.ApiCorsSettings.AllowedOrigins ?? []));
            context.Response.Headers.Append(CorsConstants.AccessControlAllowMethods,
                            string.Join(", ", app.ApiCorsSettings.AllowedMethods ?? []));
            context.Response.Headers.Append(CorsConstants.AccessControlAllowHeaders,
                             string.Join(", ", app.ApiCorsSettings.AllowedHeaders ?? []));

            if (app.ApiCorsSettings.ExposeHeaders != null)
            {
                context.Response.Headers.Append(CorsConstants.AccessControlExposeHeaders,
                            string.Join(", ", app.ApiCorsSettings.ExposeHeaders));
            }
            if (app.ApiCorsSettings.MaxAgeSeconds != null)
            {
                context.Response.Headers.Append(CorsConstants.AccessControlMaxAge,
                            app.ApiCorsSettings.MaxAgeSeconds.ToString());
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