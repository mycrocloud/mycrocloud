using System.IO.Compression;
using WebApp.Infrastructure;

namespace Api.Services;

public interface IArtifactExtractionService
{
    Task<string> ExtractAsync(Guid artifactId, Guid deploymentId, int appId);
    Task CleanupAsync(string extractedPath);
}

public class ArtifactExtractionService(
    IConfiguration config,
    AppDbContext dbContext,
    ILogger<ArtifactExtractionService> logger) : IArtifactExtractionService
{
    private readonly string _basePath = config["Deployment:ExtractBasePath"]
        ?? Path.Combine(Path.GetTempPath(), "mycrocloud", "deployments");

    public async Task<string> ExtractAsync(Guid artifactId, Guid deploymentId, int appId)
    {
        var artifact = await dbContext.Artifacts.FindAsync(artifactId);
        if (artifact?.BlobData == null)
            throw new InvalidOperationException($"Artifact {artifactId} not found or has no data");
        
        var extractPath = Path.Combine(_basePath, appId.ToString(), deploymentId.ToString());
        
        // Ensure clean directory
        if (Directory.Exists(extractPath))
            Directory.Delete(extractPath, recursive: true);
        
        Directory.CreateDirectory(extractPath);
        
        logger.LogInformation("Extracting artifact {ArtifactId} to {ExtractPath}", artifactId, extractPath);
        
        using var stream = new MemoryStream(artifact.BlobData);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.ExtractToDirectory(extractPath, overwriteFiles: true);
        
        logger.LogInformation("Extracted {EntryCount} files to {ExtractPath}", archive.Entries.Count, extractPath);
        
        return extractPath;
    }

    public Task CleanupAsync(string extractedPath)
    {
        if (Directory.Exists(extractedPath))
        {
            logger.LogInformation("Cleaning up deployment at {ExtractPath}", extractedPath);
            Directory.Delete(extractedPath, recursive: true);
        }

        return Task.CompletedTask;
    }
}
