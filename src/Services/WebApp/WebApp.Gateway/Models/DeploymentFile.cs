namespace WebApp.Gateway.Models;

/// <summary>
/// Represents a file within a deployment.
/// Links deployment to actual blob content for serving.
/// </summary>
public class DeploymentFile
{
    public Guid Id { get; set; }
    
    public Guid DeploymentId { get; set; }
    
    /// <summary>
    /// File path within deployment (e.g., "index.html", "assets/app.js")
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    public Guid BlobId { get; set; }
    public ObjectBlob Blob { get; set; } = null!;
    
    public long SizeBytes { get; set; }
    
    /// <summary>
    /// ETag for HTTP caching (typically same as Blob.ContentHash)
    /// </summary>
    public string ETag { get; set; } = string.Empty;
}
