using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionInvoker;
using FunctionSharedConstants = WebApp.FunctionInvoker.Constants;

var runtime = JsonSerializer.Deserialize<Runtime>(File.ReadAllText(Path.Combine("data", FunctionSharedConstants.RuntimeFilePath)))!;

var request = JsonSerializer.Deserialize<Request>(File.ReadAllText(Path.Combine("data", FunctionSharedConstants.RequestFilePath)))!;

var handler = File.ReadAllText(Path.Combine("data", FunctionSharedConstants.HandlerFilePath));

var executor = new JintExecutor(runtime);

var startingTimestamp = Stopwatch.GetTimestamp();
Result result;
TimeSpan duration;
try
{
    result = executor.Execute(handler, request);
}
finally
{
    duration = Stopwatch.GetElapsedTime(startingTimestamp);
}

result.Duration = duration;

var resultJson = JsonSerializer.Serialize(result);

await File.WriteAllTextAsync(Path.Combine("data", "result.json"), resultJson);