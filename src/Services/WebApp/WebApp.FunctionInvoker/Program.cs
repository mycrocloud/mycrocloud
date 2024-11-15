using System.Text.Json;
using Jint;

Result result = new();
var engine = new Engine();

var handler = File.ReadAllText("handler.js");
engine.Execute(handler);

var jsResult = engine.Evaluate("(() => { return handler(request); })();");

var statusCode = jsResult.Get("statusCode");
if (statusCode.IsNumber())
{
    result.StatusCode = (int)statusCode.AsNumber();
}

var resultJson = JsonSerializer.Serialize(result);
await File.WriteAllTextAsync("result.json", resultJson);

return;

class Result
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
    public string AdditionalLogMessage { get; set; }
}