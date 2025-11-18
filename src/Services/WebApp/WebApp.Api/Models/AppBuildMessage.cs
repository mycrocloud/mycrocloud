using System.Text.Json.Serialization;

namespace WebApp.Api.Models;

public class AppBuildMessage
{
    [JsonPropertyName("build_id")]
    public string BuildId { get; set; }
    
    [JsonPropertyName("repo_full_name")]
    public string RepoFullName { get; set; }
    
    [JsonPropertyName("clone_url")]
    public string CloneUrl { get; set; }
    
    [JsonPropertyName("branch")]
    public string Branch { get; set; }
    
    [JsonPropertyName("directory")]
    public string Directory { get; set; }
    
    [JsonPropertyName("out_dir")]
    public string OutDir { get; set; }
    
    [JsonPropertyName("install_command")]
    public string InstallCommand { get; set; }
    
    [JsonPropertyName("build_command")]
    public string BuildCommand { get; set; }
    
    [JsonPropertyName("artifacts_upload_url")]
    public string ArtifactsUploadUrl { get; set; }
}