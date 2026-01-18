using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Models;

public class SlackCommandPayload
{
    [FromForm(Name = "token")]
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    
    [FromForm(Name = "team_id")] 
    [JsonPropertyName("team_id")]
    public string? TeamId { get; set; }
    
    [FromForm(Name = "team_domain")] 
    [JsonPropertyName("team_domain")]
    public string? TeamDomain { get; set; }
    
    [FromForm(Name = "enterprise_id")]
    [JsonPropertyName("enterprise_id")]
    public string? EnterpriseId { get; set; }
    
    [FromForm(Name = "enterprise_name")] 
    [JsonPropertyName("enterprise_name")]
    public string? EnterpriseName { get; set; }
    
    [FromForm(Name = "channel_id")]
    [JsonPropertyName("channel_id")]
    public string? ChannelId { get; set; }
    
    [FromForm(Name = "channel_name")] 
    [JsonPropertyName("channel_name")]
    public string? ChannelName { get; set; }
    
    [FromForm(Name = "user_id")]
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
    
    [FromForm(Name = "user_name")]
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }
    
    [FromForm(Name = "command")] 
    [JsonPropertyName("command")]
    public string? Command { get; set; }
    
    [FromForm(Name = "text")] 
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    [FromForm(Name = "response_url")] 
    [JsonPropertyName("response_url")]
    public string? ResponseUrl { get; set; }
    
    [FromForm(Name = "trigger_id")] 
    [JsonPropertyName("trigger_id")]
    public string? TriggerId { get; set; }
    
    [FromForm(Name = "api_app_id")]
    [JsonPropertyName("api_app_id")]
    public string? ApiAppId { get; set; }
}