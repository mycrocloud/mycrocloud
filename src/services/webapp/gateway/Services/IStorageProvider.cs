namespace MycroCloud.WebApp.Gateway.Services;

/// <summary>
/// Generic interface for file storage operations.
/// Decouples the application from specific storage backends (Disk, S3, etc.).
/// </summary>
public interface IStorageProvider
{
    /// <summary>
    /// Saves a stream to the specified path.
    /// </summary>
    Task SaveAsync(string path, Stream stream, CancellationToken ct = default);

    /// <summary>
    /// Opens a stream for reading from the specified path.
    /// </summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Deletes a file at the specified path.
    /// </summary>
    Task DeleteAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Returns a full path or URI to the file, if available for the current provider.
    /// </summary>
    string GetFullPath(string path);
}
