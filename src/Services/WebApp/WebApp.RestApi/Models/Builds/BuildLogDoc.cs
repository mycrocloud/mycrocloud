using System.Text.Json.Serialization;

namespace WebApp.RestApi.Models.Builds;

public class BuildLogDoc
{
    [JsonPropertyName("@timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
    [JsonPropertyName("level")]
    public string Level { get; set; }
    
    [JsonPropertyName("source")]
    public string Source { get; set; }
}