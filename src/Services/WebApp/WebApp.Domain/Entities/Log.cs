using System.Text.Json.Serialization;

namespace WebApp.Domain.Entities;

public class Log : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    public int? RouteId { get; set; }
    public Route Route { get; set; }
    public string Method { get; set; }
    public string Path { get; set; }
    public int StatusCode { get; set; }
    public TimeSpan? FunctionExecutionDuration { get; set; }
    
    public FunctionExecutionEnvironment? FunctionExecutionEnvironment { get; set; }
    public string RemoteAddress { get; set; }
    public long? RequestContentLength { get; set; }
    public string RequestContentType { get; set; }
    public string RequestCookie { get; set; }
    public string RequestFormContent { get; set; }
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
