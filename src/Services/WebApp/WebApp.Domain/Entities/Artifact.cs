namespace WebApp.Domain.Entities;

public enum ArtifactStorageType
{
    S3 = 1,
    Disk = 2
}

/// <summary>
/// Represents a build artifact (bundle).
/// Stores metadata about the bundle and where it's stored.
/// </summary>
public class Artifact : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    
    public Guid? BuildId { get; set; }
    public AppBuild? Build { get; set; }
    
    /// <summary>
    /// SHA-256 hash of entire bundle (for deduplication at bundle level)
    /// </summary>
    public string BundleHash { get; set; } = string.Empty;
    
    public long BundleSize { get; set; }
    
    public ArtifactStorageType StorageType { get; set; }
    
    /// <summary>
    /// S3 key or disk path to bundle file
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;
    
    public string Compression { get; set; } = "zip";
    
    // Navigation
    public ICollection<SpaDeployment> Deployments { get; set; } = [];
}

