using System.Text.Json;
using WebApp.Gateway.Models;
using WebApp.Gateway.Services;
using WebApp.Gateway.Utils;

namespace WebApp.Gateway.Middlewares;

public class LoggingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<LoggingMiddleware> logger, AccessLogChannel logChannel)
    {
        await next.Invoke(context);

        if (context.Items["_AppSpecification"] is AppSpecification app && !context.Request.IsPreflightRequest())
        {
            var route = context.Items["_CachedRoute"] as CachedRoute;
            var metadata = context.Items["_ApiRouteMetadata"] as ApiRouteMetadata;
            var functionExecutionResult = context.Items["_FunctionExecutionResult"] as FunctionResult;

            var accessLog = new AccessLog
            {
                AppId = app.Id,
                RouteId = route?.Id,
                Method = context.Request.Method,
                Path = context.Request.Path + context.Request.QueryString,
                StatusCode = context.Response.StatusCode,
                FunctionLogs = functionExecutionResult?.Logs,
                FunctionRuntime = metadata?.FunctionRuntime,
                FunctionExecutionDuration = functionExecutionResult?.Duration,
                RemoteAddress = context.Connection.RemoteIpAddress!.ToString(),
                RequestContentLength = context.Request.ContentLength,
                RequestContentType = context.Request.ContentType!,
                RequestHeaders = JsonSerializer.Serialize(context.Request.Headers.ToDictionary()),
            };

            if (!logChannel.Writer.TryWrite(accessLog))
            {
                logger.LogWarning("Access log channel is full, dropping log for app {AppId}", app.Id);
            }
        }
    }
}

public static class LoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<LoggingMiddleware>();
    }
}
