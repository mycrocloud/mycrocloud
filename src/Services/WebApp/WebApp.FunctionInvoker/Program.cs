using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionInvoker;

var requestJson = File.ReadAllText("data/request.json");
var request = JsonSerializer.Deserialize<Request>(requestJson)!;

var handler = File.ReadAllText("data/handler.js");

var logger = new SafeLogger();
var executor = new JintExecutor(logger);
executor.Initialize();

var startingTimestamp = Stopwatch.GetTimestamp();
var result = new Result();
TimeSpan duration;

try
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    try
    {
        var task = Task.Run(() => executor.Execute(handler, request), cts.Token);
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
    duration = Stopwatch.GetElapsedTime(startingTimestamp);
    logger.FlushToFile("data/log");
}

result.Duration = duration;

var resultJson = JsonSerializer.Serialize(result);

await File.WriteAllTextAsync("data/result.json", resultJson);