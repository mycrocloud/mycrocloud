using System.Text.Json.Serialization;
using Nest;

namespace WebApp.RestApi.Models.Builds;

public class BuildLogDoc
{
    [JsonPropertyName("job_id")]
    [PropertyName("job_id")]
    public Guid JobId { get; set; }
    
    [JsonPropertyName("@timestamp")]
    [PropertyName("@timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("message")]
    [PropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("level")]
    [PropertyName("level")]
    public string Level { get; set; }
    
    [JsonPropertyName("source")]
    public string Source { get; set; }
}