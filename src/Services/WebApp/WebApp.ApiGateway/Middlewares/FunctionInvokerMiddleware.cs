using WebApp.ApiGateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.ApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;
        var service = new Service();

        FunctionResult result;
        
        context.Items["_FunctionExecutionEnvironment"] = route.FunctionExecutionEnvironment;
        
        switch (route.FunctionExecutionEnvironment)
        {
            case FunctionExecutionEnvironment.JintInDocker:
            {
                result = await service.ExecuteJintInDocker(context, app, appRepository, route.Response, configuration, null);
                break;
            }

            default:
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Function execution environment not supported.");
                return;
        }
        
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