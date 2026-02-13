using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Services;
using Api.Infrastructure;
using Api.Domain.Models;
using System.Text.Json;

namespace Api.Controllers;

[Route("apps/{appId:int}/api/deployments")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class ApiDeploymentsController(
    AppDbContext appDbContext,
    IApiDeploymentService apiDeploymentService,
    IAppSpecificationPublisher specPublisher,
    IStorageProvider storageProvider
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
                d.Name,
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
            deployment.Name,
            IsActive = isActive,
            deployment.Status,
            deployment.CreatedAt,
            RouteCount = routeCount,
            TotalFiles = deployment.Files.Count
        });
    }

    [HttpGet("{deploymentId:guid}/routes")]
    public async Task<IActionResult> GetRoutes(int appId, Guid deploymentId)
    {
        var deployment = await appDbContext.ApiDeployments
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound();

        // Find the routes.json file
        var routesFile = await appDbContext.DeploymentFiles
            .Include(f => f.Blob)
            .FirstOrDefaultAsync(f => f.DeploymentId == deploymentId && f.Path == "routes.json");

        if (routesFile == null)
            return NotFound(new { message = "Routes file not found for this deployment" });

        // Read and parse routes
        var stream = await storageProvider.OpenReadAsync(routesFile.Blob.StorageKey);
        using var reader = new StreamReader(stream);
        var routesContent = await reader.ReadToEndAsync();
        var routes = JsonSerializer.Deserialize<JsonElement>(routesContent);

        // Filter out functionRuntime from each route
        var filteredRoutes = new List<object>();
        if (routes.ValueKind == JsonValueKind.Array)
        {
            foreach (var route in routes.EnumerateArray())
            {
                filteredRoutes.Add(new
                {
                    name = route.TryGetProperty("name", out var n) ? n.GetString() : null,
                    method = route.TryGetProperty("method", out var m) ? m.GetString() : null,
                    path = route.TryGetProperty("path", out var p) ? p.GetString() : null,
                    description = route.TryGetProperty("description", out var d) ? d.GetString() : null,
                    responseType = route.TryGetProperty("responseType", out var rt) ? rt.GetString() : null,
                    requireAuthorization = route.TryGetProperty("requireAuthorization", out var ra) && ra.GetBoolean()
                });
            }
        }

        return Ok(filteredRoutes);
    }

    [HttpGet("{deploymentId:guid}/openapi.json")]
    public async Task<IActionResult> GetOpenApiSpec(int appId, Guid deploymentId)
    {
        var deployment = await appDbContext.ApiDeployments
            .FirstOrDefaultAsync(d => d.Id == deploymentId && d.AppId == appId);
        
        if (deployment == null)
            return NotFound();

        // Find the OpenAPI spec file
        var openApiFile = await appDbContext.DeploymentFiles
            .Include(f => f.Blob)
            .FirstOrDefaultAsync(f => f.DeploymentId == deploymentId && f.Path == "openapi.json");

        if (openApiFile == null)
            return NotFound(new { message = "OpenAPI specification not found for this deployment" });

        // Read spec from storage
        var stream = await storageProvider.OpenReadAsync(openApiFile.Blob.StorageKey);
        using var reader = new StreamReader(stream);
        var specContent = await reader.ReadToEndAsync();

        return Content(specContent, "application/json");
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishApiDeploymentRequest request)
    {
        // 1. Create the versioned snapshot of all active/enabled routes
        var deploymentId = await apiDeploymentService.CreateDeploymentSnapshotAsync(
            App.Id, 
            request.Name, 
            request.Description
        );

        // 2. Publish the new AppSpecification to Redis (which now includes the new ApiDeploymentId)
        await specPublisher.PublishAsync(App.Slug);

        return Ok(new
        {
            DeploymentId = deploymentId,
            Message = "API published successfully"
        });
    }
}

public record PublishApiDeploymentRequest(string Name, string? Description);
