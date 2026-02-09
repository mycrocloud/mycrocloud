using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Services;
using Api.Infrastructure;
using Api.Services;

namespace Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class DeploymentsController(
    AppDbContext appDbContext,
    IApiDeploymentService apiDeploymentService,
    IAppSpecificationPublisher specPublisher,
    IArtifactExtractionService extractionService
) : BaseController
{
    private App App => (HttpContext.Items["App"] as App)!;

    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var deployments = await appDbContext.SpaDeployments
            .Include(d => d.Build)
            .Include(d => d.Artifact)
            .Where(d => d.AppId == appId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        
        return Ok(deployments.Select(d => new
        {
            d.Id,
            Status = d.Status.ToString(),
            d.BuildId,
            BuildName = (string?)null, // AppBuild doesn't have Name property
            d.CreatedAt,
            ArtifactSize = d.Artifact.BundleSize,
        }));
    }

    [HttpGet("{deploymentId:guid}")]
    public async Task<IActionResult> Get(int appId, Guid deploymentId)
    {
        var deployment = await appDbContext.SpaDeployments
            .Include(d => d.Build)
            .Include(d => d.Artifact)
            .Include(d => d.App)
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound();

        return Ok(new
        {
            deployment.Id,
            Status = deployment.Status.ToString(),
            deployment.BuildId,
            BuildName = (string?)null, // AppBuild doesn't have Name property
            deployment.CreatedAt,
            ArtifactSize = deployment.Artifact.BundleSize,
            ArtifactHash = deployment.Artifact.BundleHash,
            ArtifactId = deployment.ArtifactId,
        });
    }

    [HttpPost("spa/redeploy/{artifactId:guid}")]
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

    [HttpPost("api/publish")]
    public async Task<IActionResult> PublishApi()
    {
        // 1. Create the versioned snapshot of all active/enabled routes
        var deploymentId = await apiDeploymentService.CreateDeploymentSnapshotAsync(App.Id);

        // 2. Publish the new AppSpecification to Redis (which now includes the new ApiDeploymentId)
        await specPublisher.PublishAsync(App.Slug);

        return Ok(new
        {
            DeploymentId = deploymentId,
            Message = "API published successfully"
        });
    }
}
