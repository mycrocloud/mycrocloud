using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Models;
using WebApp.Domain.Services;

namespace WebApp.Infrastructure.Services;

public class ApiDeploymentService(
    AppDbContext dbContext,
    IStorageProvider storageProvider,
    IAppSpecificationPublisher specPublisher,
    ILogger<ApiDeploymentService> logger) : IApiDeploymentService
{
    public async Task<int> BootstrapLegacyAppsAsync()
    {
        var apps = await dbContext.Apps
            .Where(a => a.ActiveApiDeploymentId == null)
            .Select(a => new { a.Id, a.Slug })
            .ToListAsync();

        logger.LogInformation("Bootstrapping API deployments for {Count} legacy apps", apps.Count);

        int count = 0;
        foreach (var app in apps)
        {
            try
            {
                await CreateDeploymentSnapshotAsync(app.Id);
                await specPublisher.PublishAsync(app.Slug);
                count++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to bootstrap app {AppId} ({Slug})", app.Id, app.Slug);
            }
        }

        return count;
    }

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
            // 1. Save Content (if exists)
            if (!string.IsNullOrEmpty(route.Response))
            {
                var content = System.Text.Encoding.UTF8.GetBytes(route.Response);
                var blob = await GetOrCreateBlobAsync(content, 
                    route.ResponseType == ResponseType.Function ? "application/javascript" : "text/plain");

                dbContext.DeploymentFiles.Add(new DeploymentFile
                {
                    Id = Guid.NewGuid(),
                    DeploymentId = deployment.Id,
                    Path = $"routes/{route.Id}/content",
                    BlobId = blob.Id,
                    SizeBytes = content.Length,
                    ETag = blob.ContentHash
                });
            }

            // 2. Save Metadata (Always exists for a route)
            var metadata = new ApiRouteMetadata
            {
                ResponseStatusCode = route.ResponseStatusCode,
                ResponseHeaders = route.ResponseHeaders ?? [],
                RequestQuerySchema = route.RequestQuerySchema,
                RequestHeaderSchema = route.RequestHeaderSchema,
                RequestBodySchema = route.RequestBodySchema,
                FunctionRuntime = route.FunctionRuntime
            };
            
            var metaContent = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metadata));
            var metaBlob = await GetOrCreateBlobAsync(metaContent, "application/json");

            dbContext.DeploymentFiles.Add(new DeploymentFile
            {
                Id = Guid.NewGuid(),
                DeploymentId = deployment.Id,
                Path = $"routes/{route.Id}/meta.json",
                BlobId = metaBlob.Id,
                SizeBytes = metaContent.Length,
                ETag = metaBlob.ContentHash
            });
        }

        // Activate the new deployment
        app.ActiveApiDeploymentId = deployment.Id;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created API Deployment snapshot {DeploymentId} for app {AppId}", deployment.Id, appId);

        return deployment.Id;
    }

    private async Task<ObjectBlob> GetOrCreateBlobAsync(byte[] content, string contentType)
    {
        var contentHash = Convert.ToHexString(SHA256.HashData(content)).ToUpperInvariant();

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
                ContentType = contentType
            };
            dbContext.ObjectBlobs.Add(blob);
            // Save immediately to avoid duplicate inserts if multiple routes have same code/meta in one snapshot
            await dbContext.SaveChangesAsync();
        }

        return blob;
    }
}
