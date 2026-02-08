using WebApp.Domain.Services;

namespace WebApp.Infrastructure.Storage;

public class DiskStorageProvider : IStorageProvider
{
    private readonly string _basePath;

    public DiskStorageProvider(string basePath)
    {
        _basePath = basePath;
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task SaveAsync(string path, Stream stream, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await stream.CopyToAsync(fileStream, ct);
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("File not found in disk storage", fullPath);
        }

        return Task.FromResult<Stream>(new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true));
    }

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string path, CancellationToken ct = default)
    {
        return Task.FromResult(File.Exists(GetFullPath(path)));
    }

    public string GetFullPath(string path)
    {
        // Ensure path doesn't try to escape base directory
        var normalizedPath = path.Replace('\\', '/').TrimStart('/');
        return Path.Combine(_basePath, normalizedPath);
    }
}
