using System.Diagnostics;
using System.Text.Json;
using Jint;
using WebApp.FunctionShared;

var runtimeText = File.ReadAllText(Path.Combine("data", "runtime.json"));
var runtime = JsonSerializer.Deserialize<Runtime>(runtimeText)!;

var mcRuntime = new MycroCloudRuntime
{
    AppId = int.Parse(Environment.GetEnvironmentVariable("APP_ID")!),
    ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")!
};

var executor = new JintExecutor(new Engine(), runtime, mcRuntime);

var handler = File.ReadAllText(Path.Combine("data", "handler.js"));

var startingTimestamp = Stopwatch.GetTimestamp();
Result result;
TimeSpan duration;
try
{
    result = executor.Execute(ReadRequest(), handler);
}
finally
{
    duration = Stopwatch.GetElapsedTime(startingTimestamp);
}

result.Duration = duration;

var resultJson = JsonSerializer.Serialize(result);

await File.WriteAllTextAsync(Path.Combine("data", "result.json"), resultJson);

Request ReadRequest()
{
    return JsonSerializer.Deserialize<Request>(File.ReadAllText(Path.Combine("data", "request.json")))!;
}

Dictionary<string, string> ReadEnv()
{
    var dictionary = new Dictionary<string, string>();
    var path = Path.Combine("data", ".env");
    if (!File.Exists(path))
    {
        return dictionary;
    }

    foreach (var line in File.ReadAllLines(path))
    {
        var parts = line.Split('=', 2);
        dictionary[parts[0]] = parts[1];
    }

    return dictionary;
}