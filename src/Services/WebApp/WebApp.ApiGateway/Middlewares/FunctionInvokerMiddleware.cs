using System.Text;
using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.FunctionShared;
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
            case FunctionExecutionEnvironment.InProcess:
            {
                result = await ExecuteInProcess(context, appRepository, app, route, configuration);
                break;
            }

            case FunctionExecutionEnvironment.OutOfProcess_DockerContainer:
            {
                result = await ExecuteOutOfProcess_DockerContainer(context, app, appRepository, route, configuration);
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

    private async Task<Result> ExecuteOutOfProcess_DockerContainer(HttpContext context, App app, IAppRepository appRepository, Route route, IConfiguration configuration)
    {
        var concurrencyJobManager = context.RequestServices.GetKeyedService<ConcurrencyJobManager>("DockerContainerFunctionExecutionManager")!;

        var dockerClient = context.RequestServices.GetRequiredService<DockerClient>();

        var hostDir = Path.Combine(configuration["DockerFunctionExecution:HostFilePath"]!, context.TraceIdentifier.Replace(':', '_'));

        Directory.CreateDirectory(hostDir);

        var runtime = new Runtime
        {
            Env = (await appRepository.GetVariables(app.Id)).ToDictionary(v => v.Name, v => v.StringValue),
        };
        
        await File.WriteAllTextAsync(Path.Combine(hostDir, FunctionSharedConstants.RuntimeFilePath), JsonSerializer.Serialize(runtime));

        await File.WriteAllTextAsync(Path.Combine(hostDir, FunctionSharedConstants.RequestFilePath), JsonSerializer.Serialize(await context.Request.Normalize()));

        await File.WriteAllTextAsync(Path.Combine(hostDir, FunctionSharedConstants.HandlerFilePath), route.Response);

        Result result;
        try
        {
            result = await concurrencyJobManager.EnqueueJob(async token =>
            {
                const string containerDataPath = "/app/data";
                var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = configuration["DockerFunctionExecution:Image"],
                    HostConfig = new HostConfig
                    {
                        AutoRemove = true,
                        Binds = [$"{hostDir}:{containerDataPath}"],
                    },
                    Env = new List<string>
                    {
                        $"{FunctionSharedConstants.APP_ID}={app.Id}",
                        $"{FunctionSharedConstants.CONNECTION_STRING}={configuration.GetConnectionString("DefaultConnection")}"
                    }
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
                    localResult.AdditionalLogMessage = await File.ReadAllTextAsync(logFilePath, token);
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

    private async Task<Result> ExecuteInProcess(HttpContext context, IAppRepository appRepository, App app, Route route, IConfiguration configuration)
    {
        var concurrencyJobManager = context.RequestServices.GetKeyedService<ConcurrencyJobManager>("InProcessFunctionExecutionManager")!;

        var logBuilder = new StringBuilder();
        var runtime = new Runtime
        {
            MemoryLimit = 5 * 1024 * 1024,
            Env = (await appRepository.GetVariables(app.Id)).ToDictionary(v => v.Name, v => v.StringValue),
            AppId = app.Id,
            ConnectionString = configuration.GetConnectionString("DefaultConnection")!,
            LogAction = o =>
            {
                var logText = o as string ?? JsonSerializer.Serialize(o);
                logBuilder.AppendLine(logText);
            }
        };
        
        var executor = new JintExecutor(runtime);

        return await concurrencyJobManager.EnqueueJob(async token =>
        {
            var request = await context.Request.Normalize();
            
            var result = executor.Execute(route.Response, request);
            result.AdditionalLogMessage = logBuilder.ToString();
            
            return result;
        }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));
    }
}

public static class FunctionInvokerMiddlewareExtensions
{
    public static IApplicationBuilder UseFunctionInvokerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FunctionInvokerMiddleware>();
    }
}