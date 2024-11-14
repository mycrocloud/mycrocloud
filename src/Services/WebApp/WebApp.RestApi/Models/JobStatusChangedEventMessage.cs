using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApp.RestApi.Models;

public enum JobStatus
{
    Started,
    Done,
    Failed,
}
public class JobStatusChangedEventMessage
{
    [JsonPropertyName("job_id")]
    public required string JobId { get; set; }
    
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }
    
    [JsonPropertyName("container_id")]
    public string? ContainerId { get; set; }
    
    [JsonPropertyName("artifacts_key_prefix")]
    public string? ArtifactsKeyPrefix { get; set; }
}