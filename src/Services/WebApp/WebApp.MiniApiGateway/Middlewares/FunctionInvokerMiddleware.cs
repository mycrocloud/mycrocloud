using System.Text;
using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.SignalR;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.FunctionShared;
using WebApp.Infrastructure;
using File = System.IO.File;
using Runtime = WebApp.FunctionShared.Runtime;
using FunctionSharedConstants = WebApp.FunctionShared.Constants;

namespace WebApp.MiniApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts, IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;

        var runnerEnvironment =
            context.Request.Headers["X-Runner-Environment"].FirstOrDefault() ?? "MycroCloudHostedRunner";

        var invocationType = context.Request.Headers["X-Invocation-Type"].FirstOrDefault() ?? "RequestResponse";

        Result result;
        
        context.Items["_FunctionExecutionEnvironment"] = route.FunctionExecutionEnvironment;
        
        switch (runnerEnvironment)
        {
            case "SelfHostedRunner":
            {
                result = await ExecuteOutOfProcess_SelfHosted(context, app, appRepository,
                    route, configuration, invocationType);
                break;
            }

            default:
            {
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

                break;
            }
        }


        //Write response
        context.Response.StatusCode = result.StatusCode ?? 200;
        foreach (var (key, value) in result.Headers)
        {
            context.Response.Headers.Append(key, value);
        }

        context.Response.Headers.Append("X-Runner-Environment", runnerEnvironment);
        context.Response.Headers.Append("X-Invocation-Type", invocationType);

        await context.Response.WriteAsync(result.Body ?? "");
        context.Items["_FunctionExecutionResult"] = result;
    }

    private async Task<Result> ExecuteOutOfProcess_SelfHosted(HttpContext context, App app,
        IAppRepository appRepository, Route route, IConfiguration configuration, string invocationType)
    {
        var hub = context.RequestServices.GetRequiredService<IHubContext<FunctionExecutionHub>>();
        var connection = FunctionExecutionHub.GetSingleConnection(app.UserId);
        if (connection is null)
        {
            return new Result
            {
                StatusCode = 500,
                Body = "You requested a function execution in a self-hosted runner, but no runner is connected."
            };
        }

        var request = await context.Request.Normalize();
        var appVars = await appRepository.GetVariables(app.Id);
        var env = new Dictionary<string, string>();
        foreach (var variable in appVars ?? Enumerable.Empty<Variable>())
        {
            env[variable.Name] = variable.StringValue;
        }

        await hub.Clients.Client(connection)
            .SendAsync("ExecuteFunction", context.TraceIdentifier, request, route.FunctionHandler, env);

        var waiter = context.RequestServices.GetRequiredService<RequestResponseWaiter>();

        var requestId = context.TraceIdentifier;

        var tcs = new TaskCompletionSource<Result>();

        waiter.Wait(requestId, tcs);

        if (invocationType != "RequestResponse")
            return new Result
            {
                StatusCode = 200,
                Body = "Function execution completed",
                Headers = new Dictionary<string, string>()
                {
                    { "X-Invocation-Type", invocationType }
                },
                AdditionalLogMessage = "Function execution completed",
                Duration = TimeSpan.Zero
            };

        var completedTask = await Task.WhenAny(tcs.Task,
            Task.Delay((app.Settings.FunctionExecutionTimeoutSeconds ?? 10) * 1000));

        if (completedTask == tcs.Task)
        {
            return tcs.Task.Result;
        }

        return new Result
        {
            StatusCode = 500,
            Body = "Function execution timeout"
        };
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

        await File.WriteAllTextAsync(Path.Combine(hostDir, FunctionSharedConstants.HandlerFilePath), route.FunctionHandler);

        return await concurrencyJobManager.EnqueueJob(async token =>
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

            var result = JsonSerializer.Deserialize<Result>(resultText)!;
            result.AdditionalLogMessage = await File.ReadAllTextAsync(Path.Combine(hostDir, "log.txt"), token);
            
            Directory.Delete(hostDir, true);

            return result;
        }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));
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
            
            var result = executor.Execute(route.FunctionHandler, request);
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