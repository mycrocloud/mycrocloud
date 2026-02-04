using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;

namespace WebApp.ApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAppRepository appRepository, FunctionExecutorFactory executorFactory)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;

        context.Items["_FunctionRuntime"] = route.FunctionRuntime;

        var executor = executorFactory.GetExecutor(route.FunctionRuntime);
        if (executor is null)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Function runtime not supported.");
            return;
        }

        var result = await executor.ExecuteAsync(context, app, appRepository, route.Response, null);

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