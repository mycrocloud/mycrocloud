using System.Diagnostics;
using System.Text.Json;
using Jint;
using WebApp.FunctionShared;
using FunctionSharedConstants = WebApp.FunctionShared.Constants;

var runtime = JsonSerializer.Deserialize<Runtime>(File.ReadAllText(Path.Combine("data", FunctionSharedConstants.RuntimeFilePath)))!;

var mcRuntime = new MycroCloudRuntime
{
    AppId = int.Parse(Environment.GetEnvironmentVariable(FunctionSharedConstants.APP_ID)!),
    ConnectionString = Environment.GetEnvironmentVariable(FunctionSharedConstants.CONNECTION_STRING)!
};

var request = JsonSerializer.Deserialize<Request>(File.ReadAllText(Path.Combine("data", FunctionSharedConstants.RequestFilePath)))!;
var handler = File.ReadAllText(Path.Combine("data", FunctionSharedConstants.HandlerFilePath));

var executor = new JintExecutor(new Engine(), runtime, mcRuntime);

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