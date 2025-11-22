using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using WebApp.ApiGateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using File = System.IO.File;

namespace WebApp.ApiGateway;

public class Service
{
    public async Task<FunctionResult> ExecuteJintInDocker(HttpContext context, App app, IAppRepository appRepository,
        string handler, IConfiguration configuration, Dictionary<string, string>? stringValues)
    {
        var concurrencyJobManager = context.RequestServices.GetKeyedService<ConcurrencyJobManager>("DockerContainerFunctionExecutionManager")!;

        var dockerClient = context.RequestServices.GetRequiredService<DockerClient>();

        var hostDir = Path.Combine(configuration["DockerFunctionExecution:HostFilePath"]!, context.TraceIdentifier.Replace(':', '_'));

        Directory.CreateDirectory(hostDir);
        
        await File.WriteAllTextAsync(Path.Combine(hostDir, "request.json"), JsonSerializer.Serialize(await context.Request.Normalize()));

        await File.WriteAllTextAsync(Path.Combine(hostDir, "handler.js"), handler);

        if (stringValues is not null)
        {
            Directory.CreateDirectory(Path.Combine(hostDir, "string_values"));
            
            foreach (var kv in stringValues)
            {
                await File.WriteAllTextAsync(Path.Combine(hostDir, "string_values", kv.Key), kv.Value);
            }
        }

        FunctionResult result;
        
        try
        {
            result = await concurrencyJobManager.EnqueueJob(async token =>
            {
                const string containerDataPath = "/app/data";

                var vars = await appRepository.GetVariables(app.Id);

                var env = vars.Select(v => $"{v.Name}={v.StringValue}").ToList();

                var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = configuration["DockerFunctionExecution:Image"],
                    HostConfig = new HostConfig
                    {
                        AutoRemove = true,
                        Binds = [$"{hostDir}:{containerDataPath}"],
                        Memory = 64 * 1024 * 1024, // 64 MB
                        NanoCPUs = 250_000_000, // 0.25 CPU (NanoCPUs = 10^9 = 1 CPU)
                        PidsLimit = 100,         //  thread / process
                        LogConfig = new LogConfig
                        {
                           Type = "fluentd",
                           Config = new Dictionary<string, string>
                           {
                               { "fluentd-address", "host.docker.internal:24224" }
                           }
                        }
                    },
                    Env = env
                }, token);

                await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters
                {
                    
                }, token);

                await dockerClient.Containers.WaitContainerAsync(container.ID, token);

                var resultText = await File.ReadAllTextAsync(Path.Combine(hostDir, "result.json"), token);

                var innerResult = JsonSerializer.Deserialize<FunctionResult>(resultText)!;
                
                var logFilePath = Path.Combine(hostDir, "log");
                
                if (File.Exists(logFilePath))
                {
                    innerResult.Log = await File.ReadAllTextAsync(logFilePath, token);
                }

                Directory.Delete(hostDir, true);

                return innerResult;
            }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));
        }
        catch (TaskCanceledException)
        {
            result = new FunctionResult
            {
                StatusCode = 500,
                Body = "Timeout",
            };
        }

        return result;
    }
}