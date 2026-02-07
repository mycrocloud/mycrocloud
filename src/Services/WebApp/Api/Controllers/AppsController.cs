using Api.Filters;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using WebApp.Domain.Services;
using WebApp.Infrastructure;
using Api.Extensions;
using Api.Models.Apps;

namespace Api.Controllers;

[TypeFilter<AppOwnerActionFilter>(Arguments = ["id"])]
public class AppsController(
    IAppService appService,
    IAppRepository appRepository,
    AppDbContext appDbContext,
    GitHubAppService githubAppService,
    ISubscriptionService subscriptionService,
    IAppCacheInvalidator cacheInvalidator
) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var apps = await appRepository.ListByUserId(User.GetUserId(), q, "");
        
        return Ok(apps.Select(app => new
        {
            app.Id,
            app.Name,
            app.Description,
            Status = app.Status.ToString(),
            app.CreatedAt,
            app.UpdatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(AppCreateRequest appCreateRequest)
    {
        var nameExists = await appDbContext.Apps.AnyAsync(a => a.Name == appCreateRequest.Name);
        if (nameExists)
        {
            return Conflict(new { Message = "App name already exists" });
        }

        var appCount = await appDbContext.Apps.CountAsync(a => a.UserId == User.GetUserId());
        if (!await subscriptionService.CanCreateApp(User.GetUserId(), appCount))
        {
            return BadRequest(new { Message = "You have reached the limit of 10 apps" });
        }

        var app = appCreateRequest.ToEntity();

        await appService.Create(User.GetUserId(), app);

        return Created(app.Id.ToString(), new
        {
            app.Id,
            app.CreatedAt,
            app.Version
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var app = await appRepository.GetByAppId(id);
        
        Response.Headers.Append(ETagHeader, app.Version.ToString());
        
        return Ok(new
        {
            app.Id,
            app.Name,
            app.Description,
            Status = app.Status.ToString(),
            app.CreatedAt,
            app.UpdatedAt,
            app.Version
        });
    }

    [HttpPatch("{id:int}/Rename")]
    public async Task<IActionResult> Rename(int id, AppRenameRequest renameRequest)
    {
        var app = await appRepository.GetByAppId(id);

        if (app is null) return BadRequest();

        if (!User.GetUserId().Equals(app.UserId)) return BadRequest();

        var currentETag = app.Version.ToString();
        var requestETag = Request.Headers[IfMatchHeader].ToString();
        if (requestETag != currentETag)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed);
        }

        var nameTaken = await appDbContext.Apps.AnyAsync(a => a.Id != id && a.Name == renameRequest.Name);
        if (nameTaken)
        {
            return Conflict(new { Message = "App name already taken" });
        }

        var oldName = app.Name;
        await appService.Rename(id, renameRequest.Name);
        await cacheInvalidator.InvalidateAsync(oldName); // Invalidate old name
        return NoContent();
    }

    [HttpPatch("{id:int}/Status")]
    public async Task<IActionResult> SetStatus(int id, AppStatus status)
    {
        await appService.SetStatus(id, status);
        await cacheInvalidator.InvalidateByIdAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await cacheInvalidator.InvalidateByIdAsync(id); // Invalidate before deletion
        await appService.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:int}/cors")]
    public async Task<IActionResult> Cors(int id, CorsSettings settings)
    {
        await appService.SetCorsSettings(id, settings);
        await cacheInvalidator.InvalidateByIdAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/cors")]
    public async Task<IActionResult> Cors(int id)
    {
        var app = await appRepository.GetByAppId(id);
        return Ok(app.CorsSettings);
    }
    
    [HttpGet("{id:int}/link")]
    public async Task<IActionResult> Link(int id)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Link)
            .ThenInclude(i => i.GitHubInstallation)
            .SingleAsync(a => a.Id == id && a.UserId == User.GetUserId());

        if (app.Link is not { } link)
        {
            return NotFound();
        }
        
        return Ok(new
        {
            Type = "GitHub", //TODO: 
            Org = link.GitHubInstallation.AccountLogin, 
            link.RepoId,
            Repo = link.RepoName
        });
    }

    [HttpPost("{id:int}/link/github")]
    public async Task<IActionResult> ConnectGitHubRepo(int id, GitHubRepoIntegrationRequest request)
    {
        var installation = await appDbContext.GitHubInstallations
            .Where(i => i.InstallationId == request.InstallationId && i.UserId == User.GetUserId())
            .SingleAsync();

        var repos = await githubAppService.GetAccessibleRepos(installation.InstallationId);
        
        var repo = repos.Single(r => r.Id == request.RepoId);

        var app = await appDbContext.Apps
            .Include(a => a.Link)
            .SingleAsync(a => a.Id == id);
        
        app.Link = new AppLink
        {
            InstallationId = installation.InstallationId,
            RepoId = repo.Id,
            RepoName = repo.Name
        };
        
        await appDbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}/link")]
    public async Task<IActionResult> DisconnectGitHubRepo(int id)
    {
        var app = await appDbContext.Apps
            .Include(app => app.Link)
            .SingleAsync(a => a.Id == id);

        appDbContext.Remove(app.Link);

        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpPost("{appId}/routing-config")]
    public async Task<IActionResult> UpdateRoutingConfig(int appId, [FromBody] UpdateRoutingConfigRequest request)
    {
        var config = new WebApp.Domain.Entities.RoutingConfig
        {
            SchemaVersion = request.SchemaVersion,
            Routes = request.Routes.Select(r => new RoutingConfigRoute
            {
                Name = r.Name,
                Priority = r.Priority,
                Match = new WebApp.Domain.Entities.RouteMatch
                {
                    Type = (WebApp.Domain.Entities.RouteMatchType)r.Match.Type,
                    Path = r.Match.Path
                },
                Target = new WebApp.Domain.Entities.RouteTarget
                {
                    Type = (WebApp.Domain.Entities.RouteTargetType)r.Target.Type,
                    StripPrefix = r.Target.StripPrefix,
                    Rewrite = r.Target.Rewrite,
                    Fallback = r.Target.Fallback
                }
            }).ToList()
        };

        await appService.SetRoutingConfig(appId, config);
        await cacheInvalidator.InvalidateByIdAsync(appId);
        return NoContent();
    }

    [HttpGet("{appId}/routing-config")]
    public async Task<IActionResult> GetRoutingConfig(int appId)
    {
        var app = await appRepository.GetByAppId(appId);
        var config = app.RoutingConfig is { Routes.Count: > 0 }
            ? app.RoutingConfig
            : WebApp.Domain.Entities.RoutingConfig.Default;

        return Ok(new
        {
            config.SchemaVersion,
            Routes = config.Routes.Select(r => new
            {
                r.Name,
                r.Priority,
                Match = new
                {
                    Type = r.Match.Type.ToString(),
                    r.Match.Path
                },
                Target = new
                {
                    Type = r.Target.Type.ToString(),
                    r.Target.StripPrefix,
                    r.Target.Rewrite,
                    r.Target.Fallback
                }
            })
        });
    }
}

public class GitHubRepoIntegrationRequest
{
    public long InstallationId { get; set; }
    public long RepoId { get; set; }
}