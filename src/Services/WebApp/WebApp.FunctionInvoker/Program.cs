using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionInvoker;
using WebApp.FunctionInvoker.Apis.Fetch;

var result = new Result();
var startingTimestamp = Stopwatch.GetTimestamp();
try
{
    var executor = new JintExecutor();
    executor.Initialize();
    var console = executor.Console;

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
    catch (CountLimitException ex)
    {
        console.Error("CountLimitException");
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