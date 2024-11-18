using System.Diagnostics;
using System.Text.Json;
using WebApp.FunctionShared;

IExecutor executor = new JintExecutor();

var handler = File.ReadAllText(Path.Combine("data", "handler.js"));

var startingTimestamp = Stopwatch.GetTimestamp();
Result result;
TimeSpan duration;
try
{
    result = executor.Execute(ReadRequest(), handler, ReadEnv());
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