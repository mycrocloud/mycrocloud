using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.FunctionInvoker.Apis.Console;

public class Console(
    string logFilePath = "log.json",
    int maxLogsPerSecond = 10,
    int maxLogLength = 500,
    int maxTotalLogs = 100)
    : IDisposable
{
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();
    private readonly object _lockObj = new();
    private DateTime _lastResetTime = DateTime.UtcNow;
    private int _logCountInCurrentSecond = 0;

    public IReadOnlyList<LogEntry> Logs
    {
        get
        {
            lock (_lockObj)
            {
                return _logQueue.ToList();
            }
        }
    }

    public void Info(object? message)
    {
        Log(LogType.Info, message);
    }

    public void Warn(object? message)
    {
        Log(LogType.Warning, message);
    }

    public void Error(object? message)
    {
        Log(LogType.Error, message);
    }

    private void Log(LogType type, object? message)
    {
        lock (_lockObj)
        {
            var now = DateTime.UtcNow;
                
            // Reset counter if 1 second has passed
            if ((now - _lastResetTime).TotalSeconds >= 1)
            {
                _lastResetTime = now;
                _logCountInCurrentSecond = 0;
            }

            // Check rate limit - silently ignore if exceeded
            if (_logCountInCurrentSecond >= maxLogsPerSecond)
            {
                return;
            }

            // Format message
            var formattedMessage = FormatObject(message);

            // Truncate if too long
            if (formattedMessage.Length > maxLogLength)
            {
                formattedMessage = formattedMessage.Substring(0, maxLogLength) + "... [truncated]";
            }

            // Add log entry
            _logQueue.Enqueue(new LogEntry
            {
                Message = formattedMessage,
                Timestamp = now,
                Type = type
            });

            _logCountInCurrentSecond++;

            // Enforce total log limit
            while (_logQueue.Count > maxTotalLogs)
            {
                _logQueue.TryDequeue(out _);
            }
        }
    }

    private string FormatObject(object? obj)
    {
        if (obj == null) return "null";
        if (obj is string str) return str;
            
        // Handle Jint.Native.JsValue without direct reference
        var objType = obj.GetType();
        if (objType.FullName?.StartsWith("Jint.Native") == true)
        {
            try
            {
                var isNullMethod = objType.GetMethod("IsNull");
                var isUndefinedMethod = objType.GetMethod("IsUndefined");
                var isObjectMethod = objType.GetMethod("IsObject");
                var toStringMethod = objType.GetMethod("ToString");

                if (isNullMethod?.Invoke(obj, null) is bool isNull && isNull)
                    return "null";
                    
                if (isUndefinedMethod?.Invoke(obj, null) is bool isUndefined && isUndefined)
                    return "undefined";
                    
                if (isObjectMethod?.Invoke(obj, null) is bool isObject && isObject)
                {
                    var wrapperType = Type.GetType("Jint.Runtime.Interop.ObjectWrapper, Jint");
                    var stringifyMethod = wrapperType?.GetMethod("Stringify");
                    if (stringifyMethod != null)
                    {
                        return stringifyMethod.Invoke(null, new[] { obj })?.ToString() ?? "[Object]";
                    }
                    return "[Object]";
                }
                    
                return toStringMethod?.Invoke(obj, null)?.ToString() ?? obj.ToString() ?? "null";
            }
            catch
            {
                return obj.ToString() ?? "[Object]";
            }
        }
            
        return obj.ToString() ?? "null";
    }

    public void Clear()
    {
        lock (_lockObj)
        {
            while (_logQueue.TryDequeue(out _)) { }
            _logCountInCurrentSecond = 0;
            _lastResetTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Write all logs to JSON file
    /// </summary>
    private void SaveToFile()
    {
        lock (_lockObj)
        {
            try
            {
                var logs = _logQueue.ToList();
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };

                var json = JsonSerializer.Serialize(logs, options);
                File.WriteAllText(logFilePath, json);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Failed to save logs to {logFilePath}: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        SaveToFile();
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