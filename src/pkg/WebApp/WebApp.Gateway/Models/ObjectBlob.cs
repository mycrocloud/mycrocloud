namespace WebApp.Gateway.Models;

public enum BlobStorageType
{
    S3 = 1,
    Disk = 2
}

/// <summary>
/// Content-addressable blob storage for individual files.
/// Supports deduplication via content hash.
/// </summary>
public class ObjectBlob
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
    /// S3 key or disk path
    /// </summary>
    public string StorageKey { get; set; } = string.Empty;
}
