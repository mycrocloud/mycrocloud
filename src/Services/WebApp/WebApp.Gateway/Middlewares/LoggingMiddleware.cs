using System.Text.Json;
using WebApp.Domain.Models;
using WebApp.Gateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;

namespace WebApp.Gateway.Middlewares;

public class LoggingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<LoggingMiddleware> logger, ILogRepository logRepository)
    {
        await next.Invoke(context);

        if (context.Items["_AppSpecification"] is AppSpecification app && !context.Request.IsPreflightRequest())
        {
            var route = context.Items["_CachedRoute"] as CachedRoute;
            var metadata = context.Items["_ApiRouteMetadata"] as ApiRouteMetadata;
            var functionExecutionResult = context.Items["_FunctionExecutionResult"] as FunctionResult;

            await logRepository.Add(new Log
            {
                AppId = app.Id,
                RouteId = route?.Id,
                Method = context.Request.Method,
                Path = context.Request.Path + context.Request.QueryString,
                StatusCode = context.Response.StatusCode,
                FunctionLogs = functionExecutionResult?.Logs,
                FunctionRuntime = metadata?.FunctionRuntime,
                FunctionExecutionDuration = functionExecutionResult?.Duration,
                RemoteAddress = context.Connection.RemoteIpAddress?.ToString(),
                RequestContentLength = context.Request.ContentLength,
                RequestContentType = context.Request.ContentType,
                RequestCookie = JsonSerializer.Serialize(context.Request.Cookies.ToDictionary()),
                RequestFormContent = context.Request.HasFormContentType
                    ? JsonSerializer.Serialize(context.Request.Form.ToDictionary())
                    : null,
                RequestHeaders = JsonSerializer.Serialize(context.Request.Headers.ToDictionary()),
            });
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