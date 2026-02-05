using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using WebApp.Gateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using File = System.IO.File;

namespace WebApp.Gateway;

public class DockerFunctionExecutor(
    [FromKeyedServices("DockerFunctionExecution")] ConcurrentJobQueue jobQueue,
    DockerClient dockerClient,
    IConfiguration configuration) : IFunctionExecutor
{
    public FunctionRuntime Runtime => FunctionRuntime.JintInDocker;

    public async Task<FunctionResult> ExecuteAsync(HttpContext context, App app, IAppRepository appRepository,
        string handler, Dictionary<string, string>? values)
    {
        var hostDir = Path.Combine(configuration["DockerFunctionExecution:HostFilePath"]!, context.TraceIdentifier.Replace(':', '_'));

        Directory.CreateDirectory(hostDir);

        await File.WriteAllTextAsync(Path.Combine(hostDir, "request.json"), JsonSerializer.Serialize(await context.Request.Normalize()));

        await File.WriteAllTextAsync(Path.Combine(hostDir, "handler.js"), handler);

        if (values is not null)
        {
            var valuesJson = JsonSerializer.Serialize(values);
            await File.WriteAllTextAsync(Path.Combine(hostDir, "values.json"), valuesJson);
        }

        FunctionResult result;

        try
        {
            result = await jobQueue.EnqueueAsync(async token =>
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
                    },
                    Env = env
                }, token);

                await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters
                {

                }, token);

                await dockerClient.Containers.WaitContainerAsync(container.ID, token);

                var resultText = await File.ReadAllTextAsync(Path.Combine(hostDir, "result.json"), token);

                var innerResult = JsonSerializer.Deserialize<FunctionResult>(resultText)!;

                var logFilePath = Path.Combine(hostDir, "log.json");

                if (File.Exists(logFilePath))
                {
                    var logJson = await File.ReadAllTextAsync(logFilePath, token);
                    innerResult.Logs = JsonSerializer.Deserialize<List<FunctionLogEntry>>(logJson)!;
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
