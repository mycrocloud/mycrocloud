using System.Text.Json.Serialization;

namespace WebApp.ApiGateway.Models;

public class FunctionResult
{
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public TimeSpan Duration { get; set; }
    public string Log { get; set; }
    
    public ICollection<LogEntry> Logs { get; set; }
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