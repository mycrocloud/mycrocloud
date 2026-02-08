using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Services;

namespace WebApp.Infrastructure.Services;

public class ApiDeploymentService(
    AppDbContext dbContext,
    IStorageProvider storageProvider,
    ILogger<ApiDeploymentService> logger) : IApiDeploymentService
{
    public async Task<Guid> CreateDeploymentSnapshotAsync(int appId)
    {
        var app = await dbContext.Apps
            .Include(a => a.Routes.Where(r => r.Enabled && r.Status == RouteStatus.Active))
            .FirstOrDefaultAsync(a => a.Id == appId);

        if (app == null) throw new InvalidOperationException("App not found");

        var deployment = new ApiDeployment
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            Status = DeploymentStatus.Ready // Marked as Ready immediately for snapshots
        };

        dbContext.ApiDeployments.Add(deployment);

        foreach (var route in app.Routes)
        {
            if (string.IsNullOrEmpty(route.Response)) continue;

            var content = System.Text.Encoding.UTF8.GetBytes(route.Response);
            var contentHash = Convert.ToHexString(SHA256.HashData(content)).ToUpperInvariant();

            // Handle Blob deduplication
            var blob = await dbContext.ObjectBlobs.FirstOrDefaultAsync(b => b.ContentHash == contentHash);
            if (blob == null)
            {
                var storageKey = $"blobs/{contentHash.Substring(0, 2)}/{contentHash}";
                using var ms = new MemoryStream(content);
                await storageProvider.SaveAsync(storageKey, ms);

                blob = new ObjectBlob
                {
                    Id = Guid.NewGuid(),
                    ContentHash = contentHash,
                    SizeBytes = content.Length,
                    StorageType = BlobStorageType.Disk,
                    StorageKey = storageKey,
                    ContentType = route.ResponseType == ResponseType.Function ? "application/javascript" : "text/plain"
                };
                dbContext.ObjectBlobs.Add(blob);
                // Save immediately to avoid duplicate inserts if multiple routes have same code in one snapshot
                await dbContext.SaveChangesAsync(); 
            }

            // Create DeploymentFile indexed by RouteId convention
            var deploymentFile = new DeploymentFile
            {
                Id = Guid.NewGuid(),
                DeploymentId = deployment.Id,
                Path = $"routes/{route.Id}/content",
                BlobId = blob.Id,
                SizeBytes = content.Length,
                ETag = contentHash
            };
            dbContext.DeploymentFiles.Add(deploymentFile);
        }

        // Activate the new deployment
        app.ActiveApiDeploymentId = deployment.Id;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created API Deployment snapshot {DeploymentId} for app {AppId}", deployment.Id, appId);

        return deployment.Id;
    }
}
