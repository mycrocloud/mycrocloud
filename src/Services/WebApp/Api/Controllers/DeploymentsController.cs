using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Api.Domain.Entities;
using Api.Domain.Services;
using WebApp.Infrastructure;

namespace Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class DeploymentsController(
    AppDbContext appDbContext,
    IApiDeploymentService apiDeploymentService,
    IAppSpecificationPublisher specPublisher
) : BaseController
{
    private App App => (HttpContext.Items["App"] as App)!;

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
