using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Services;
using Api.Infrastructure;

namespace Api.Controllers;

[Route("apps/{appId:int}/api/deployments")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class ApiDeploymentsController(
    AppDbContext appDbContext,
    IApiDeploymentService apiDeploymentService,
    IAppSpecificationPublisher specPublisher
) : BaseController
{
    private App App => (HttpContext.Items["App"] as App)!;

    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var app = App;
        var activeDeploymentId = app.ActiveApiDeploymentId;
        
        var deployments = await appDbContext.ApiDeployments
            .Where(d => d.AppId == appId)
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new
            {
                d.Id,
                IsActive = d.Id == activeDeploymentId,
                d.Status,
                d.CreatedAt,
                RouteCount = appDbContext.DeploymentFiles
                    .Count(f => f.DeploymentId == d.Id && f.Path.StartsWith("routes/"))
            })
            .ToListAsync();
        
        return Ok(deployments);
    }

    [HttpGet("{deploymentId:guid}")]
    public async Task<IActionResult> Get(int appId, Guid deploymentId)
    {
        var deployment = await appDbContext.ApiDeployments
            .Include(d => d.App)
            .Include(d => d.Files)
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound();

        var isActive = deployment.Id == deployment.App.ActiveApiDeploymentId;
        var routeCount = deployment.Files.Count(f => f.Path.StartsWith("routes/"));

        return Ok(new
        {
            deployment.Id,
            IsActive = isActive,
            deployment.Status,
            deployment.CreatedAt,
            RouteCount = routeCount,
            TotalFiles = deployment.Files.Count
        });
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish()
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
