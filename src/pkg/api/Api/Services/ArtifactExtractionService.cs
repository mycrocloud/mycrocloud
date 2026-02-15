using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Services;
using Api.Infrastructure;

namespace Api.Services;

public interface IArtifactExtractionService
{
    Task<string> ExtractAsync(Guid artifactId, Guid deploymentId, int appId);
}

public class ArtifactExtractionService(
    AppDbContext dbContext,
    IStorageProvider storageProvider,
    IConfiguration configuration,
    ILogger<ArtifactExtractionService> logger) : IArtifactExtractionService
{
    public async Task<string> ExtractAsync(Guid artifactId, Guid deploymentId, int appId)
    {
        var artifact = await dbContext.Artifacts.FindAsync(artifactId);
        if (artifact == null)
            throw new InvalidOperationException($"Artifact {artifactId} not found");

        var deployment = await dbContext.SpaDeployments.FindAsync(deploymentId);
        if (deployment == null)
            throw new InvalidOperationException($"Deployment {deploymentId} not found");

        logger.LogInformation("Extracting artifact {ArtifactId} for deployment {DeploymentId}", artifactId, deploymentId);

        var blobStorageType = (configuration["Storage:Type"] ?? "Disk").Equals("S3", StringComparison.OrdinalIgnoreCase)
            ? BlobStorageType.S3
            : BlobStorageType.Disk;

        using var artifactStream = await storageProvider.OpenReadAsync(artifact.StorageKey);
        using var archive = new ZipArchive(artifactStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // Skip directories

            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            await entryStream.CopyToAsync(ms);
            var content = ms.ToArray();

            var contentHash = Convert.ToHexString(SHA256.HashData(content)).ToUpperInvariant();

            // Check if blob already exists
            var blob = await dbContext.ObjectBlobs.FirstOrDefaultAsync(b => b.ContentHash == contentHash);

            if (blob == null)
            {
                var storageKey = $"blobs/{contentHash.Substring(0, 2)}/{contentHash}";

                ms.Position = 0;
                await storageProvider.SaveAsync(storageKey, ms);

                blob = new ObjectBlob
                {
                    Id = Guid.NewGuid(),
                    ContentHash = contentHash,
                    SizeBytes = content.Length,
                    StorageType = blobStorageType,
                    StorageKey = storageKey
                };
                dbContext.ObjectBlobs.Add(blob);
                await dbContext.SaveChangesAsync(); // Save immediately to avoid duplicate inserts if multiple files have same hash in one zip
            }

            // Create deployment file entry
            var deploymentFile = new DeploymentFile
            {
                Id = Guid.NewGuid(),
                DeploymentId = deploymentId,
                Path = entry.FullName,
                BlobId = blob.Id,
                SizeBytes = content.Length,
                ETag = contentHash
            };
            dbContext.DeploymentFiles.Add(deploymentFile);
        }

        deployment.Status = DeploymentStatus.Ready;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Extracted {EntryCount} files for deployment {DeploymentId}", archive.Entries.Count, deploymentId);

        return "success";
    }
}
