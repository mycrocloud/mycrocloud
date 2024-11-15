using System.Text.Json;
using WebApp.FunctionShared;

IExecutor executor = new JintExecutor();

var request = JsonSerializer.Deserialize<Request>(File.ReadAllText(Path.Combine("data", "request.json")))!;
var handler = File.ReadAllText(Path.Combine("data", "handler.js"));

var response = executor.Execute(request, handler);

var resultJson = JsonSerializer.Serialize(response);
await File.WriteAllTextAsync(Path.Combine("data", "result.json"), resultJson);