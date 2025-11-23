using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionInvoker;

var result = new Result();
var logger = new SafeLogger();
var startingTimestamp = Stopwatch.GetTimestamp();
try
{
    var executor = new JintExecutor(logger);
    executor.Initialize();

    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    try
    {
        var task = Task.Run(() => executor.Execute(), cts.Token);
        task.Wait(cts.Token);
        result = task.Result;
    }
    catch (OperationCanceledException)
    {
        logger.Log("Execution timed out.");
    }
    catch (Exception ex)
    {
        logger.Log("Execution error: " + ex.Message);
    }
}
finally
{
    result.Duration = Stopwatch.GetElapsedTime(startingTimestamp);
}

logger.FlushToFile("data/log");

var resultJson = JsonSerializer.Serialize(result);
await File.WriteAllTextAsync("data/result.json", resultJson);