using System.Text.Json.Serialization;
using Api.Domain.Enums;

namespace Api.Domain.Entities;

public class AccessLog
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? RouteId { get; set; }
    public string Method { get; set; }
    public string Path { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan? FunctionExecutionDuration { get; set; }
    public FunctionRuntime? FunctionRuntime { get; set; }
    public string RemoteAddress { get; set; }
    public long? RequestContentLength { get; set; }
    public string RequestContentType { get; set; }
    public string RequestHeaders { get; set; }
    public ICollection<FunctionLogEntry> FunctionLogs { get; set; }
}

public class FunctionLogEntry
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
