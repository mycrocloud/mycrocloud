using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Jint;
using Jint.Native;
using Microsoft.AspNetCore.SignalR;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.FunctionShared;
using WebApp.FunctionShared.PlugIns;
using WebApp.Infrastructure;
using File = System.IO.File;

namespace WebApp.MiniApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts,
        IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;

        var runnerEnvironment =
            context.Request.Headers["X-Runner-Environment"].FirstOrDefault() ?? "MycroCloudHostedRunner";

        var invocationType = context.Request.Headers["X-Invocation-Type"].FirstOrDefault() ?? "RequestResponse";

        Result result;

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
                        result = await ExecuteInProcess(context, scripts, appRepository, app,
                            route);
                        break;
                    }

                    case FunctionExecutionEnvironment.OutOfProcess_DockerContainer:
                    {
                        result = await ExecuteOutOfProcess_DockerContainer(context, app, appRepository,
                            route, configuration);
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

    private async Task<Result> ExecuteOutOfProcess_DockerContainer(HttpContext context, App app,
        IAppRepository appRepository, Route route,
        IConfiguration configuration)
    {
        var concurrencyJobManager =
            context.RequestServices.GetKeyedService<ConcurrencyJobManager>(
                "DockerContainerFunctionExecutionManager")!;

        var dockerClient = context.RequestServices.GetRequiredService<DockerClient>();

        var hostDir =
            Path.Combine(configuration["DockerFunctionExecution:HostFilePath"]!,
                context.TraceIdentifier.Replace(':', '_'));

        Directory.CreateDirectory(hostDir);

        await File.WriteAllTextAsync(Path.Combine(hostDir, "request.json"),
            JsonSerializer.Serialize(await context.Request.Normalize()));

        // Inject environment variables
        var appVariables = await appRepository.GetVariables(app.Id);
        var env = new StringBuilder();
        foreach (var variable in appVariables)
        {
            env.AppendLine($"{variable.Name}={variable.StringValue}");
        }

        await File.WriteAllTextAsync(Path.Combine(hostDir, ".env"), env.ToString());

        await File.WriteAllTextAsync(Path.Combine(hostDir, "handler.js"), route.FunctionHandler);

        var containerFilePath = "/app/data";

        return await concurrencyJobManager.EnqueueJob(async token =>
        {
            var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters()
            {
                Image = configuration["DockerFunctionExecution:Image"],
                HostConfig = new HostConfig
                {
                    AutoRemove = true,
                    Binds = [$"{hostDir}:{containerFilePath}"]
                }
            }, token);

            await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters()
            {
            }, token);

            await dockerClient.Containers.WaitContainerAsync(container.ID, token);

            var text = await File.ReadAllTextAsync(Path.Combine(hostDir, "result.json"), token);

            Directory.Delete(hostDir, true);

            return JsonSerializer.Deserialize<Result>(text)!;
        }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));
    }


    private async Task<Result> ExecuteInProcess(HttpContext context, Scripts scripts,
        IAppRepository appRepository, App app, Route route)
    {
        var concurrencyJobManager =
            context.RequestServices.GetKeyedService<ConcurrencyJobManager>(
                "InProcessFunctionExecutionManager")!;

        var engine = new Engine(options =>
        {
            if (app.Settings.CheckFunctionExecutionLimitMemory)
            {
                var memoryLimit = app.Settings.FunctionExecutionLimitMemoryBytes ?? 1 * 1024 * 1024;
                memoryLimit += 10 * 1024 * 1024;

                options.LimitMemory(memoryLimit);
            }
        });

        //Inject global variables
        await InjectEnvironmentVariables(appRepository, app, engine);

        // Inject utility scripts
        InjectBuiltInUtilityScripts(scripts, engine);

        //Inject user-defined dependencies
        await InjectUserDefinedDependencies(route, engine);

        //Inject plugins
        using var scope = context.RequestServices.CreateScope();
        var dbContext2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        InjectPlugIns(engine, dbContext2, app);

        JsValue jsValue;
        Exception? exception = null;
        TimeSpan duration;

        //Start measuring time for function execution
        var startingTimestamp = Stopwatch.GetTimestamp();
        try
        {
            engine.SetRequestValue(await context.Request.Normalize());
            const string code = "(() => { return $FunctionHandler$(request); })();";

            jsValue = await concurrencyJobManager.EnqueueJob(token =>
            {
                engine.Execute(route.FunctionHandler);
                var value = engine.Evaluate(code.Replace("$FunctionHandler$",
                    route.FunctionHandlerMethod ?? "handler"));

                return Task.FromResult(value);
            }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));
        }
        catch (Exception e)
        {
            jsValue = JsValue.FromObject(engine, new
            {
                statusCode = 500,
                body = e.Message,
                additionalLogMessage = e.Message
            });
            exception = e;
        }
        finally
        {
            duration = Stopwatch.GetElapsedTime(startingTimestamp);
        }

        var result = JintExecutor.Map(jsValue);
        result.Exception = exception;
        result.Duration = duration;

        return result;
    }

    private static void InjectPlugIns(Engine engine, AppDbContext dbContext, App app)
    {
        engine.SetValue("useTextStorage",
            new Func<string, TextStorageAdapter>(name => new TextStorageAdapter(app, name, dbContext)));

        engine.SetValue("useObjectStorage",
            () => new ObjectStorageAdapter(app.Id, dbContext));
    }

    private async Task InjectUserDefinedDependencies(Route route, Engine engine)
    {
        foreach (var dependency in route.FunctionHandlerDependencies ?? [])
        {
            var script = await LoadScript(dependency);
            if (!string.IsNullOrEmpty(script))
            {
                engine.Execute(script);
            }
        }
    }

    private static void InjectBuiltInUtilityScripts(Scripts scripts, Engine engine)
    {
        //engine.Execute(scripts.Faker);
        engine.Execute(scripts.Handlebars);
        engine.Execute(scripts.Lodash);
    }

    private static async Task InjectEnvironmentVariables(IAppRepository appRepository, App app, Engine engine)
    {
        var variables = new Dictionary<string, object?>();
        var appVariables = await appRepository.GetVariables(app.Id);
        foreach (var variable in appVariables)
        {
            object? value = variable.ValueType switch
            {
                VariableValueType.String => variable.StringValue,
                VariableValueType.Number => int.Parse(variable.StringValue),
                VariableValueType.Boolean => bool.Parse(variable.StringValue),
                VariableValueType.Null => null,
                _ => throw new ArgumentOutOfRangeException()
            };
            variables[variable.Name] = value;
        }

        engine.SetValue("env", variables);
    }

    private async Task<string> LoadScript(string dependency)
    {
        return string.Empty;
    }
}

public static class FunctionInvokerMiddlewareExtensions
{
    public static IApplicationBuilder UseFunctionInvokerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FunctionInvokerMiddleware>();
    }
}