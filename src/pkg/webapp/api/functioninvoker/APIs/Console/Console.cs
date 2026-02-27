using System.Text.Json;
using System.Text.Json.Serialization;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;

namespace WebApp.FunctionInvoker.Apis.Console;

public class Console(
    int maxLogsPerSecond = 10,
    int maxLogLength = 500,
    int maxTotalLogs = 100)
{
    private readonly List<LogEntry> _logs = [];
    private DateTime _lastResetTime = DateTime.UtcNow;
    private int _logCountInCurrentSecond;

    public IReadOnlyList<LogEntry> Logs => _logs;

    public void Info(object? message) => Log(LogType.Info, message);

    public void Warn(object? message) => Log(LogType.Warning, message);

    public void Error(object? message) => Log(LogType.Error, message);

    private void Log(LogType type, object? message)
    {
        var now = DateTime.UtcNow;

        if ((now - _lastResetTime).TotalSeconds >= 1)
        {
            _lastResetTime = now;
            _logCountInCurrentSecond = 0;
        }

        if (_logCountInCurrentSecond >= maxLogsPerSecond)
            return;

        var formatted = FormatObject(message);

        if (formatted.Length > maxLogLength)
            formatted = formatted[..maxLogLength] + "... [truncated]";

        _logs.Add(new LogEntry
        {
            Message = formatted,
            Timestamp = now,
            Type = type
        });

        _logCountInCurrentSecond++;

        while (_logs.Count > maxTotalLogs)
            _logs.RemoveAt(0);
    }

    private static string FormatObject(object? obj)
    {
        if (obj is null) return "null";
        if (obj is string s) return s;

        if (obj is JsValue js)
        {
            if (js.IsNull()) return "null";
            if (js.IsUndefined()) return "undefined";
            if (js.IsBoolean()) return js.AsBoolean() ? "true" : "false";
            if (js.IsNumber()) return js.AsNumber().ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (js.IsString()) return js.AsString();
            if (js is JsArray arr)
            {
                var items = new List<string>();
                foreach (var item in arr)
                    items.Add(FormatObject(item));
                return $"[{string.Join(", ", items)}]";
            }
            if (js.IsObject())
            {
                try
                {
                    var jsObj = js.AsObject();
                    var props = new List<string>();
                    foreach (var pair in jsObj.GetOwnProperties())
                        props.Add($"{pair.Key}: {FormatObject(pair.Value.Value)}");
                    return $"{{ {string.Join(", ", props)} }}";
                }
                catch
                {
                    return "[Object]";
                }
            }
            return js.ToString();
        }

        return obj.ToString() ?? "null";
    }
}

public class LogEntry
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogType Type { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LogType
{
    Info,
    Warning,
    Error
}
