using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using WebApp.ApiGateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;
using File = System.IO.File;
using Runtime = WebApp.FunctionShared.Runtime;
using FunctionSharedConstants = WebApp.FunctionShared.Constants;

namespace WebApp.ApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts, IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;

        Result result;
        
        context.Items["_FunctionExecutionEnvironment"] = route.FunctionExecutionEnvironment;
        
        switch (route.FunctionExecutionEnvironment)
        {
            case FunctionExecutionEnvironment.JintInDocker:
            {
                result = await ExecuteJintInDocker(context, app, appRepository, route, configuration);
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

    private async Task<Result> ExecuteJintInDocker(HttpContext context, App app, IAppRepository appRepository, Route route, IConfiguration configuration)
    {
        var concurrencyJobManager = context.RequestServices.GetKeyedService<ConcurrencyJobManager>("DockerContainerFunctionExecutionManager")!;

        var dockerClient = context.RequestServices.GetRequiredService<DockerClient>();

        var hostDir = Path.Combine(configuration["DockerFunctionExecution:HostFilePath"]!, context.TraceIdentifier.Replace(':', '_'));

        Directory.CreateDirectory(hostDir);
        
        
        await File.WriteAllTextAsync(Path.Combine(hostDir, ""), JsonSerializer.Serialize(await context.Request.Normalize()));

        await File.WriteAllTextAsync(Path.Combine(hostDir, ""), route.Response);

        Result result;
        
        try
        {
            result = await concurrencyJobManager.EnqueueJob(async token =>
            {
                const string containerDataPath = "/app/data";
                
                var env = new List<string>();
                var vars = await appRepository.GetVariables(app.Id);
                
                foreach (var v in vars)
                {
                    var key = v.Name?.Trim();
                    var value = v.StringValue?.Trim() ?? "";

                    if (string.IsNullOrEmpty(key)) continue;
                    if (key.StartsWith("#")) continue;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                        continue;

                    env.Add($"{key}={value}");
                }
                
                var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = configuration["DockerFunctionExecution:Image"],
                    HostConfig = new HostConfig
                    {
                        AutoRemove = true,
                        Binds = [$"{hostDir}:{containerDataPath}"],
                    },
                    Env = env
                }, token);

                await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters
                {

                }, token);

                await dockerClient.Containers.WaitContainerAsync(container.ID, token);

                var resultText = await File.ReadAllTextAsync(Path.Combine(hostDir, "result.json"), token);

                var localResult = JsonSerializer.Deserialize<Result>(resultText)!;
                var logFilePath = Path.Combine(hostDir, "log.txt");
                
                if (File.Exists(logFilePath))
                {
                    localResult.Log = await File.ReadAllTextAsync(logFilePath, token);
                }

                Directory.Delete(hostDir, true);

                return localResult;
            }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));
        }
        catch (TaskCanceledException)
        {
            result = new Result
            {
                StatusCode = 500,
                Body = "Timeout",
            };
        }

        return result;
    }
}

public static class FunctionInvokerMiddlewareExtensions
{
    public static IApplicationBuilder UseFunctionInvokerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FunctionInvokerMiddleware>();
    }
}