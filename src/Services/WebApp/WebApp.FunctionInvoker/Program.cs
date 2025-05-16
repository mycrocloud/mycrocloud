using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionShared;
using FunctionSharedConstants = WebApp.FunctionShared.Constants;

var runtime = JsonSerializer.Deserialize<Runtime>(File.ReadAllText(Path.Combine("data", FunctionSharedConstants.RuntimeFilePath)))!;

runtime.AppId = int.Parse(Environment.GetEnvironmentVariable(FunctionSharedConstants.APP_ID)!);
runtime.ConnectionString = Environment.GetEnvironmentVariable(FunctionSharedConstants.CONNECTION_STRING)!;
runtime.LogAction = obj =>
{
    var logText = obj as string ?? JsonSerializer.Serialize(obj);
    File.AppendAllText(Path.Combine("data", "log.txt"), logText + Environment.NewLine);
};

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