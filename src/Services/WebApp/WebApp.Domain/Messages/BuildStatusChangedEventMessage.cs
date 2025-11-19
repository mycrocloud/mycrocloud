using System.Text.Json.Serialization;

namespace WebApp.Domain.Messages;

public enum BuildStatus
{
    Started,
    Done,
    Failed,
}

public class BuildStatusChangedMessage
{
    [JsonPropertyName("build_id")]
    public required Guid BuildId { get; set; }
    
    [JsonPropertyName("status")]
    public BuildStatus Status { get; set; }
    
    [JsonPropertyName("container_id")]
    public string? ContainerId { get; set; }
}