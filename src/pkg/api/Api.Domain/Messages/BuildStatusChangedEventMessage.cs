using System.Text.Json.Serialization;

namespace Api.Domain.Messages;

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
    
    [JsonPropertyName("artifact_id")]
    public Guid? ArtifactId { get; set; }
}