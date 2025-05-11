using System.Text.Json.Serialization;

namespace WebApp.Api.Models;

public class AppBuildMessage
{
    [JsonPropertyName("job_id")]
    public string JobId { get; set; }
    
    [JsonPropertyName("repo_full_name")]
    public string RepoFullName { get; set; }
    
    [JsonPropertyName("clone_url")]
    public string CloneUrl { get; set; }
    
    [JsonPropertyName("directory")]
    public string Directory { get; set; }
    
    [JsonPropertyName("out_dir")]
    public string OutDir { get; set; }
    
    [JsonPropertyName("install_command")]
    public string InstallCommand { get; set; }
    
    [JsonPropertyName("build_command")]
    public string BuildCommand { get; set; }
}