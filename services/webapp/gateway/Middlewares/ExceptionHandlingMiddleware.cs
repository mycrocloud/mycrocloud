using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Middlewares;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var appId = (context.Items["_AppSpecification"] as AppSpecification)?.Id;
            logger.LogError(ex, "Unhandled exception: {Method} {Path}, AppId={AppId}",
                context.Request.Method, context.Request.Path, appId);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal Server Error");
            }
        }
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
