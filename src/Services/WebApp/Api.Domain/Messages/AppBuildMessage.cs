using System.Text.Json.Serialization;

namespace Api.Domain.Messages;

/// <summary>
/// Resource limits based on account plan
/// </summary>
public class PlanLimits
{
    [JsonPropertyName("memory_mb")]
    public int MemoryMB { get; set; }

    [JsonPropertyName("cpu_percent")]
    public int CPUPercent { get; set; }

    [JsonPropertyName("build_timeout_s")]
    public int BuildTimeoutS { get; set; }

    [JsonPropertyName("artifact_size_mb")]
    public int ArtifactSizeMB { get; set; }

    /// <summary>
    /// Default limits for free tier
    /// </summary>
    public static PlanLimits Free => new()
    {
        MemoryMB = 1024,        // 1 GB
        CPUPercent = 100,       // 1 core
        BuildTimeoutS = 600,    // 10 min
        ArtifactSizeMB = 100    // 100 MB
    };

    /// <summary>
    /// Limits for Pro tier
    /// </summary>
    public static PlanLimits Pro => new()
    {
        MemoryMB = 2048,        // 2 GB
        CPUPercent = 200,       // 2 cores
        BuildTimeoutS = 1800,   // 30 min
        ArtifactSizeMB = 500    // 500 MB
    };

    /// <summary>
    /// Limits for Enterprise tier
    /// </summary>
    public static PlanLimits Enterprise => new()
    {
        MemoryMB = 4096,        // 4 GB
        CPUPercent = 400,       // 4 cores
        BuildTimeoutS = 3600,   // 60 min
        ArtifactSizeMB = 1024   // 1 GB
    };
}

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

    [JsonPropertyName("node_version")]
    public string NodeVersion { get; set; }

    [JsonPropertyName("env_vars")]
    public Dictionary<string, string> EnvVars { get; set; } = new();

    [JsonPropertyName("artifacts_upload_url")]
    public string ArtifactsUploadUrl { get; set; }

    [JsonPropertyName("limits")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlanLimits? Limits { get; set; }
}