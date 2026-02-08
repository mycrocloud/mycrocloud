namespace WebApp.Domain.Entities;

public enum BlobStorageType
{
    Database = 1,
    S3 = 2,
    Disk = 3
}

/// <summary>
/// Content-addressable blob storage for individual files.
/// Supports deduplication via content hash.
/// </summary>
public class ObjectBlob : BaseEntity
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// SHA-256 hash of file content (deduplication key)
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;
    
    public long SizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    
    public BlobStorageType StorageType { get; set; }
    
    /// <summary>
    /// S3 key or disk path (when StorageType != Database)
    /// </summary>
    public string? StorageKey { get; set; }
    
    /// <summary>
    /// Blob data (only when StorageType == Database)
    /// </summary>
    public byte[]? BlobData { get; set; }
    
    // Navigation
    public ICollection<DeploymentFile> DeploymentFiles { get; set; } = [];
}
