using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionInvoker;
using WebApp.FunctionInvoker.Apis.Fetch;
using FunctionConsole = WebApp.FunctionInvoker.Apis.Console.Console;

var result = new Result();
FunctionConsole? console = null;
var startingTimestamp = Stopwatch.GetTimestamp();
try
{
    using var executor = new JintExecutor();
    executor.Initialize();
    console = executor.Console;

    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    try
    {
        var task = Task.Run(() => executor.Execute(), cts.Token);
        task.Wait(cts.Token);
        result = task.Result;
    }
    catch (OperationCanceledException)
    {
        console.Error("Execution timed out.");
    }
    catch (CountLimitException)
    {
        console.Error("Fetch request limit exceeded (max 50 requests per execution).");
    }
    catch (FetchSecurityException ex)
    {
        console.Error("Fetch security error: " + ex.Message);
    }
    catch (FetchSizeLimitException ex)
    {
        console.Error("Fetch size limit error: " + ex.Message);
    }
    catch (FetchTimeoutException ex)
    {
        console.Error("Fetch timeout: " + ex.Message);
    }
    catch (Exception ex)
    {
        console.Error("Execution error: " + ex.Message);
    }
}
finally
{
    result.Duration = Stopwatch.GetElapsedTime(startingTimestamp);
}

var resultJson = JsonSerializer.Serialize(result);
await File.WriteAllTextAsync("data/result.json", resultJson);

if (console?.Logs.Count > 0)
{
    var logJson = JsonSerializer.Serialize(console.Logs);
    await File.WriteAllTextAsync("data/log.json", logJson);
}
