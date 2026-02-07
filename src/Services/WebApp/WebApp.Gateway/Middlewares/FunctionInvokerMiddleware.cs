using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppCacheService appCacheService, FunctionExecutorFactory executorFactory)
    {
        var app = (CachedApp)context.Items["_CachedApp"]!;
        var route = (CachedRoute)context.Items["_CachedRoute"]!;

        context.Items["_FunctionRuntime"] = route.FunctionRuntime;

        var executor = executorFactory.GetExecutor(route.FunctionRuntime);
        if (executor is null)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Function runtime not supported.");
            return;
        }

        // Load function code from DB (not cached)
        var functionCode = await appCacheService.GetRouteResponseAsync(route.Id);
        if (string.IsNullOrEmpty(functionCode))
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Function code not found.");
            return;
        }

        var result = await executor.ExecuteAsync(context, app, functionCode, null);

        await context.Response.WriteFromFunctionResult(result);

        context.Items["_FunctionExecutionResult"] = result;
    }
}

public static class FunctionInvokerMiddlewareExtensions
{
    public static IApplicationBuilder UseFunctionInvokerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FunctionInvokerMiddleware>();
    }
}