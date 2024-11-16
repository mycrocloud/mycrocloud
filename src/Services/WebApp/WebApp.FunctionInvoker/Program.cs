using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionShared;

IExecutor executor = new JintExecutor();

var request = JsonSerializer.Deserialize<Request>(File.ReadAllText(Path.Combine("data", "request.json")))!;
var handler = File.ReadAllText(Path.Combine("data", "handler.js"));

var startingTimestamp = Stopwatch.GetTimestamp();
Result result;
TimeSpan duration;
try
{
    result = executor.Execute(request, handler);
}
finally
{
    duration = Stopwatch.GetElapsedTime(startingTimestamp);
}

result.Duration = duration;

var resultJson = JsonSerializer.Serialize(result);

await File.WriteAllTextAsync(Path.Combine("data", "result.json"), resultJson);