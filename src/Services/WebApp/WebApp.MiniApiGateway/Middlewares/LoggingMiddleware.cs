using System.Text.Json;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using WebApp.FunctionShared;

namespace WebApp.MiniApiGateway.Middlewares;

public class LoggingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogger<LoggingMiddleware> logger, ILogRepository logRepository,
        IRouteRepository routeRepository, IConfiguration configuration)
    {
        await next.Invoke(context);

        if (context.Items["_App"] is App app && !context.Request.IsPreflightRequest())
        {
            var route = context.Items["_Route"] as Route;
            var functionExecutionResult = context.Items["_FunctionExecutionResult"] as Result;
            
            FunctionExecutionEnvironment? functionExecutionEnvironment = null;
            if (context.Items.TryGetValue("_FunctionExecutionEnvironment", out var value) &&
                value is FunctionExecutionEnvironment env)
            {
                functionExecutionEnvironment = env;
            }

            await logRepository.Add(new Log
            {
                App = app,
                Route = route,
                Method = context.Request.Method,
                Path = context.Request.Path + context.Request.QueryString,
                StatusCode = context.Response.StatusCode,
                AdditionalLogMessage = functionExecutionResult?.AdditionalLogMessage,
                FunctionExecutionEnvironment = functionExecutionEnvironment,
                FunctionExecutionDuration = functionExecutionResult?.Duration,
                RemoteAddress = context.Request.Headers[configuration["RemoteAddressHeader"]!].ToString(),
                RequestContentLength = context.Request.ContentLength,
                RequestContentType = context.Request.ContentType,
                RequestCookie = JsonSerializer.Serialize(context.Request.Cookies.ToDictionary()),
                RequestFormContent = context.Request.HasFormContentType
                    ? JsonSerializer.Serialize(context.Request.Form.ToDictionary())
                    : null,
                RequestHeaders = JsonSerializer.Serialize(context.Request.Headers.ToDictionary()),
            });

            if (functionExecutionResult?.Exception is { } e)
            {
                if (e is TimeoutException && route is not null)
                {
                    route.Status = RouteStatus.Blocked;
                    await routeRepository.Update(route.Id, route);
                }
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