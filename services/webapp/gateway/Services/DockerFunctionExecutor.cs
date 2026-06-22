using System.Diagnostics;
using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using MycroCloud.WebApp.Gateway.Models;
using MycroCloud.WebApp.Gateway.Utils;
using File = System.IO.File;

namespace MycroCloud.WebApp.Gateway.Services;

public class DockerFunctionExecutor(
    [FromKeyedServices("DockerFunctionExecution")] ConcurrentJobQueue jobQueue,
    DockerClient dockerClient,
    IConfiguration configuration,
    ILogger<DockerFunctionExecutor> logger) : IFunctionExecutor
{
    public FunctionRuntime Runtime => FunctionRuntime.JintInDocker;

    public async Task<FunctionResult> ExecuteAsync(HttpContext context, AppSpecification app,
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

        var timeoutSeconds = app.Settings.FunctionExecutionTimeoutSeconds ?? 10;

        // Stage diagnostics: which step the job was in and how long each step took.
        // Read in the timeout handler below to pinpoint where a slow execution stalled.
        var jobStopwatch = new Stopwatch();
        var stage = "queued";
        TimeSpan createElapsed = default, startElapsed = default, waitElapsed = default;

        // Use cached variables
        var env = app.Variables.Select(v => $"{v.Name}={v.Value}").ToList();

        // Propagate recursion depth
        var currentDepth = 0;
        if (context.Request.Headers.TryGetValue("X-MycroCloud-Depth", out var depthHeader)
            && int.TryParse(depthHeader.FirstOrDefault(), out var parsedDepth))
        {
            currentDepth = parsedDepth;
        }
        env.Add($"MYCROCLOUD_FUNCTION_DEPTH={currentDepth}");

        // Forward proxy for outbound fetch (hides server IP)
        var fetchProxy = configuration["DockerFunctionExecution:FetchProxy"];
        if (!string.IsNullOrEmpty(fetchProxy))
        {
            env.Add($"MYCROCLOUD_FETCH_PROXY={fetchProxy}");
        }

        try
        {
            result = await jobQueue.EnqueueAsync(async token =>
            {
                const string containerDataPath = "/app/data";

                // Job is now running (semaphore acquired); the timeout clock starts here.
                jobStopwatch.Restart();
                var stageStopwatch = Stopwatch.StartNew();

                stage = "create";
                var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Image = configuration["DockerFunctionExecution:Image"],
                    HostConfig = new HostConfig
                    {
                        Binds = [$"{hostDir}:{containerDataPath}"],
                        Memory = 64 * 1024 * 1024, // 64 MB
                        NanoCPUs = 250_000_000, // 0.25 CPU (NanoCPUs = 10^9 = 1 CPU)
                        PidsLimit = 100,         //  thread / process
                    },
                    Env = env
                }, token);
                createElapsed = stageStopwatch.Elapsed;

                try
                {
                    stage = "start";
                    stageStopwatch.Restart();
                    await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters(), token);
                    startElapsed = stageStopwatch.Elapsed;

                    stage = "wait";
                    stageStopwatch.Restart();
                    var waitResponse = await dockerClient.Containers.WaitContainerAsync(container.ID, token);
                    waitElapsed = stageStopwatch.Elapsed;

                    stage = "read-result";

                    // Capture container logs for diagnostics
                    string containerLogs = "";
                    try
                    {
                        var logStream = await dockerClient.Containers.GetContainerLogsAsync(container.ID,
                            false, new ContainerLogsParameters { ShowStdout = true, ShowStderr = true }, token);
                        var (stdout, stderr) = await logStream.ReadOutputToEndAsync(token);
                        containerLogs = stdout + stderr;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to retrieve container logs for app {AppId}", app.Id);
                    }

                    if (waitResponse.StatusCode != 0)
                    {
                        logger.LogError("Container exited with code {ExitCode} for app {AppId}. Logs:\n{ContainerLogs}",
                            waitResponse.StatusCode, app.Id, containerLogs);
                    }

                    var resultPath = Path.Combine(hostDir, "result.json");
                    if (!File.Exists(resultPath))
                    {
                        var files = string.Join(", ", Directory.GetFiles(hostDir).Select(Path.GetFileName));
                        logger.LogError("result.json missing after container exited with code {ExitCode} for app {AppId}. " +
                            "Files in hostDir: [{Files}]. Container logs:\n{ContainerLogs}",
                            waitResponse.StatusCode, app.Id, files, containerLogs);
                        return new FunctionResult { StatusCode = 500, Body = "Function produced no result." };
                    }

                    var resultText = await File.ReadAllTextAsync(resultPath, token);
                    var innerResult = JsonSerializer.Deserialize<FunctionResult>(resultText)!;

                    var logFilePath = Path.Combine(hostDir, "log.json");
                    if (File.Exists(logFilePath))
                    {
                        var logJson = await File.ReadAllTextAsync(logFilePath, token);
                        innerResult.Logs = JsonSerializer.Deserialize<List<FunctionLogEntry>>(logJson)!;
                    }

                    return innerResult;
                }
                finally
                {
                    try { await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, CancellationToken.None); }
                    catch { /* best-effort removal */ }
                }
            }, TimeSpan.FromSeconds(timeoutSeconds));
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning(
                "Function execution timed out for app {AppId} (trace {TraceId}) after {ElapsedMs}ms at stage '{Stage}' " +
                "(budget {TimeoutSeconds}s). Stage timings: create={CreateMs}ms, start={StartMs}ms, wait={WaitMs}ms.",
                app.Id, context.TraceIdentifier, jobStopwatch.ElapsedMilliseconds, stage, timeoutSeconds,
                createElapsed.TotalMilliseconds, startElapsed.TotalMilliseconds, waitElapsed.TotalMilliseconds);

            result = new FunctionResult
            {
                StatusCode = 500,
                Body = "Timeout",
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Function execution failed for app {app.Id}", ex);
        }
        finally
        {
            try { Directory.Delete(hostDir, true); } catch { /* best-effort cleanup */ }
        }

        return result;
    }
}
