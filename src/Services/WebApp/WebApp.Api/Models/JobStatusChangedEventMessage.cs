using System.Text.Json.Serialization;

namespace WebApp.Api.Models;

public enum JobStatus
{
    Started,
    Done,
    Failed,
}
public class JobStatusChangedEventMessage
{
    [JsonPropertyName("job_id")]
    public required Guid JobId { get; set; }
    
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }
    
    [JsonPropertyName("container_id")]
    public string? ContainerId { get; set; }
}