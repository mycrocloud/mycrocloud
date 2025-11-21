using WebApp.ApiGateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.ApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts, IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;
        var service = new Service();

        Result result;
        
        context.Items["_FunctionExecutionEnvironment"] = route.FunctionExecutionEnvironment;
        
        switch (route.FunctionExecutionEnvironment)
        {
            case FunctionExecutionEnvironment.JintInDocker:
            {
                result = await service.ExecuteJintInDocker(context, app, appRepository, route.Response, configuration);
                break;
            }

            default:
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Function execution environment not supported.");
                return;
        }
        
        //Write response
        context.Response.StatusCode = result.StatusCode ?? 200;
        foreach (var (key, value) in result.Headers)
        {
            context.Response.Headers.Append(key, value);
        }

        await context.Response.WriteAsync(result.Body ?? "");
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