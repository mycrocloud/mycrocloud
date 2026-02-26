using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Infrastructure;
using Api.Services;
using Api.Domain.Services;

namespace Api.Controllers;

[Route("apps/{appId:int}/spa/deployments")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class SpaDeploymentsController(
    AppDbContext appDbContext,
    IArtifactExtractionService extractionService,
    IStorageProvider storageProvider
) : BaseController
{
    private App App => (HttpContext.Items["App"] as App)!;

    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var app = App;
        var activeDeploymentId = app.ActiveSpaDeploymentId;
        
        var deployments = await appDbContext.SpaDeployments
            .Include(d => d.Build)
            .Include(d => d.Artifact)
            .Where(d => d.AppId == appId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        
        return Ok(deployments.Select(d => new
        {
            d.Id,
            IsActive = d.Id == activeDeploymentId,
            d.Name,
            Status = d.Status.ToString(),
            d.BuildId,
            Build = d.Build != null ? new
            {
                Metadata = d.Build.Metadata
            } : null,
            d.CreatedAt,
            ArtifactSize = d.Artifact != null ? d.Artifact.BundleSize : (long?)null,
        }));
    }

    [HttpGet("{deploymentId:guid}")]
    public async Task<IActionResult> Get(int appId, Guid deploymentId)
    {
        var deployment = await appDbContext.SpaDeployments
            .Include(d => d.Build)
            .Include(d => d.Artifact)
            .Include(d => d.App)
                .ThenInclude(a => a.ActiveSpaDeployment)
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound();

        var isActive = deployment.Id == deployment.App.ActiveSpaDeploymentId;

        return Ok(new
        {
            deployment.Id,
            IsActive = isActive,
            deployment.Name,
            Status = deployment.Status.ToString(),
            deployment.BuildId,
            Build = deployment.Build != null ? new
            {
                Metadata = deployment.Build.Metadata
            } : null,
            deployment.CreatedAt,
            ArtifactSize = deployment.Artifact?.BundleSize,
            ArtifactHash = deployment.Artifact?.BundleHash,
            ArtifactId = deployment.ArtifactId,
        });
    }

    [HttpGet("{deploymentId:guid}/files")]
    public async Task<IActionResult> GetFiles(int appId, Guid deploymentId, [FromQuery] string? search = null)
    {
        var deployment = await appDbContext.SpaDeployments
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound();

        var query = appDbContext.DeploymentFiles
            .Include(f => f.Blob)
            .Where(f => f.DeploymentId == deploymentId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(f => f.Path.Contains(search));
        }

        var files = await query
            .OrderBy(f => f.Path)
            .Select(f => new
            {
                f.Path,
                f.SizeBytes,
                f.ETag,
                ContentType = f.Blob.ContentType,
                f.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            TotalFiles = files.Count,
            TotalSize = files.Sum(f => f.SizeBytes),
            Files = files
        });
    }

    [HttpGet("{deploymentId:guid}/download")]
    public async Task<IActionResult> DownloadArtifact(int appId, Guid deploymentId)
    {
        var deployment = await appDbContext.SpaDeployments
            .Include(d => d.Artifact)
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound("Deployment not found");
        
        if (deployment.ArtifactId == null || deployment.Artifact == null)
            return NotFound("Artifact not found for this deployment");

        var artifact = deployment.Artifact;
        var stream = await storageProvider.OpenReadAsync(artifact.StorageKey);
        
        // Generate filename from deployment ID or build name
        var fileName = $"deployment-{deploymentId.ToString()[..8]}.zip";
        
        return File(stream, "application/zip", fileName);
    }

    [HttpPost("redeploy/{artifactId:guid}")]
    public async Task<IActionResult> RedeployArtifact(int appId, Guid artifactId)
    {
        var artifact = await appDbContext.Artifacts
            .FirstOrDefaultAsync(a => a.Id == artifactId && a.AppId == appId);
        
        if (artifact == null)
            return NotFound("Artifact not found");

        // Create new deployment from existing artifact (rollback/redeploy scenario)
        var deployment = new SpaDeployment
        {
            Id = Guid.NewGuid(),
            AppId = appId,
            BuildId = null, // No build - this is a redeploy
            ArtifactId = artifact.Id,
            Status = DeploymentStatus.Pending
        };
        
        appDbContext.SpaDeployments.Add(deployment);
        await appDbContext.SaveChangesAsync();

        // Extract artifact asynchronously
        try
        {
            await extractionService.ExtractAsync(artifact.Id, deployment.Id, appId);
            deployment.Status = DeploymentStatus.Ready;
        }
        catch (Exception ex)
        {
            deployment.Status = DeploymentStatus.Failed;
            await appDbContext.SaveChangesAsync();
            return BadRequest(new { Message = "Failed to extract artifact", Error = ex.Message });
        }

        await appDbContext.SaveChangesAsync();

        return Ok(new
        {
            DeploymentId = deployment.Id,
            Message = "Artifact redeployed successfully"
        });
    }
}
