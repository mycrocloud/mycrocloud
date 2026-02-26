using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Models;
using Api.Domain.Services;

namespace Api.Infrastructure.Services;

public class ApiDeploymentService(
    AppDbContext dbContext,
    IStorageProvider storageProvider,
    IAppSpecificationPublisher specPublisher,
    IOpenApiGenerator openApiGenerator,
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

    public async Task<Guid> CreateDeploymentSnapshotAsync(int appId, string? name = null, string? description = null)
    {
        var app = await dbContext.Apps
            .Include(a => a.Routes.Where(r => r.Enabled && r.Status == RouteStatus.Active))
            .FirstOrDefaultAsync(a => a.Id == appId);

        if (app == null) throw new InvalidOperationException("App not found");

        var deployment = new ApiDeployment
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            Name = name,
            Description = description,
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
                Id = route.Id,
                Name = route.Name,
                Method = route.Method,
                Path = route.Path,
                Description = route.Description,
                ResponseType = route.ResponseType,
                Response = new ApiRouteResponseMetadata
                {
                    StaticResponse = route.ResponseType == ResponseType.Static
                        ? new ApiStaticResponseMetadata
                        {
                            StatusCode = route.ResponseStatusCode,
                            Headers = route.ResponseHeaders ?? []
                        }
                        : null,
                    FunctionResponse = route.ResponseType == ResponseType.Function
                        ? new ApiFunctionResponseMetadata
                        {
                            Runtime = route.FunctionRuntime
                        }
                        : null
                },
                RequestQuerySchema = route.RequestQuerySchema,
                RequestHeaderSchema = route.RequestHeaderSchema,
                RequestBodySchema = route.RequestBodySchema,
                RequireAuthorization = route.RequireAuthorization
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

        // 3. Generate and save OpenAPI specification
        var routeMetadataList = app.Routes.Select(route => new ApiRouteMetadata
        {
            Id = route.Id,
            Name = route.Name,
            Method = route.Method,
            Path = route.Path,
            Description = route.Description,
            ResponseType = route.ResponseType,
            Response = new ApiRouteResponseMetadata
            {
                StaticResponse = route.ResponseType == ResponseType.Static
                    ? new ApiStaticResponseMetadata
                    {
                        StatusCode = route.ResponseStatusCode,
                        Headers = route.ResponseHeaders ?? []
                    }
                    : null,
                FunctionResponse = route.ResponseType == ResponseType.Function
                    ? new ApiFunctionResponseMetadata
                    {
                        Runtime = route.FunctionRuntime
                    }
                    : null
            },
            RequestQuerySchema = route.RequestQuerySchema,
            RequestHeaderSchema = route.RequestHeaderSchema,
            RequestBodySchema = route.RequestBodySchema,
            RequireAuthorization = route.RequireAuthorization
        }).ToList();

        var openApiSpec = openApiGenerator.GenerateSpecification(app.Slug, app.Slug, routeMetadataList);
        var openApiContent = System.Text.Encoding.UTF8.GetBytes(openApiSpec);
        var openApiBlob = await GetOrCreateBlobAsync(openApiContent, "application/json");

        dbContext.DeploymentFiles.Add(new DeploymentFile
        {
            Id = Guid.NewGuid(),
            DeploymentId = deployment.Id,
            Path = "openapi.json",
            BlobId = openApiBlob.Id,
            SizeBytes = openApiContent.Length,
            ETag = openApiBlob.ContentHash
        });

        // 4. Generate and save simplified routes.json for UI display
        var routesSummary = routeMetadataList.Select(r => new
        {
            name = r.Name,
            method = r.Method,
            path = r.Path,
            description = r.Description,
            responseType = r.ResponseType.ToString(),
            requireAuthorization = r.RequireAuthorization,
            functionRuntime = r.Response.FunctionResponse?.Runtime?.ToString()
        }).ToList();

        var routesJson = JsonSerializer.Serialize(routesSummary, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var routesContent = System.Text.Encoding.UTF8.GetBytes(routesJson);
        var routesBlob = await GetOrCreateBlobAsync(routesContent, "application/json");

        dbContext.DeploymentFiles.Add(new DeploymentFile
        {
            Id = Guid.NewGuid(),
            DeploymentId = deployment.Id,
            Path = "routes.json",
            BlobId = routesBlob.Id,
            SizeBytes = routesContent.Length,
            ETag = routesBlob.ContentHash
        });

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
