using Api.Filters;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;
using Api.Domain.Services;
using Api.Infrastructure;
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
    IAppCacheInvalidator cacheInvalidator,
    IAppSpecificationPublisher specPublisher
) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var apps = await appRepository.ListByUserId(User.GetUserId(), q, "");
        
        return Ok(apps.Select(app => new
        {
            app.Id,
            app.Slug,
            Name = app.Slug,
            app.Description,
            State = app.State.ToString(),
            app.CreatedAt,
            app.UpdatedAt
        }));
    }

    [HttpPost]
    public async Task<IActionResult> Create(AppCreateRequest appCreateRequest)
    {
        var nameExists = await appDbContext.Apps.AnyAsync(a => a.Slug == appCreateRequest.Name);
        if (nameExists)
        {
            return Conflict(new { Message = "App name already exists" });
        }

        var appCount = await appDbContext.Apps.CountAsync(a => a.OwnerId == User.GetUserId());
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
        var app = await appDbContext.Apps
            .Include(a => a.ActiveSpaDeployment)
            .Include(a => a.ActiveApiDeployment)
            .Include(a => a.Link)
            .ThenInclude(l => l.GitHubInstallation)
            .FirstAsync(a => a.Id == id);

        Response.Headers.Append(ETagHeader, app.Version.ToString());

        var activeSpa = app.ActiveSpaDeployment;
        var activeApi = app.ActiveApiDeployment;

        return Ok(new
        {
            app.Id,
            app.Slug,
            Name = app.Slug,
            app.Description,
            State = app.State.ToString(),
            app.CreatedAt,
            app.UpdatedAt,
            app.Version,
            ActiveSpaDeployment = activeSpa != null ? new
            {
                activeSpa.Id,
                activeSpa.Name,
                Status = activeSpa.Status.ToString(),
                activeSpa.CreatedAt
            } : null,
            ActiveApiDeployment = activeApi != null ? new
            {
                activeApi.Id,
                Status = activeApi.Status.ToString(),
                activeApi.CreatedAt
            } : null,
            GitIntegration = app.Link != null ? new
            {
                Provider = "GitHub",
                Org = app.Link.GitHubInstallation.AccountLogin,
                app.Link.RepoId,
                Repo = app.Link.RepoName
            } : null
        });
    }

    [HttpPatch("{id:int}/Rename")]
    public async Task<IActionResult> Rename(int id, AppRenameRequest renameRequest)
    {
        var app = await appRepository.GetByAppId(id);

        if (app is null) return BadRequest();

        if (!User.GetUserId().Equals(app.OwnerId)) return BadRequest();

        var currentETag = app.Version.ToString();
        var requestETag = Request.Headers[IfMatchHeader].ToString();
        if (requestETag != currentETag)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed);
        }

        var nameTaken = await appDbContext.Apps.AnyAsync(a => a.Id != id && a.Slug == renameRequest.Name);
        if (nameTaken)
        {
            return Conflict(new { Message = "App name already taken" });
        }

        var oldName = app.Slug;
        await appService.Rename(id, renameRequest.Name);
        await cacheInvalidator.InvalidateAsync(oldName); // Invalidate old name
        await specPublisher.InvalidateAsync(oldName); // Invalidate spec with old name
        await specPublisher.PublishAsync(renameRequest.Name); // Publish with new name
        return NoContent();
    }

    [HttpPatch("{id:int}/State")]
    public async Task<IActionResult> SetState(int id, AppState state)
    {
        await appService.SetState(id, state);
        var app = await appRepository.GetByAppId(id);
        await cacheInvalidator.InvalidateByIdAsync(id);
        await specPublisher.PublishAsync(app.Slug);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var app = await appRepository.GetByAppId(id);
        await cacheInvalidator.InvalidateByIdAsync(id); // Invalidate before deletion
        await specPublisher.InvalidateAsync(app.Slug);
        await appService.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:int}/cors")]
    public async Task<IActionResult> Cors(int id, CorsSettings settings)
    {
        await appService.SetCorsSettings(id, settings);
        var app = await appRepository.GetByAppId(id);
        await cacheInvalidator.InvalidateByIdAsync(id);
        await specPublisher.PublishAsync(app.Slug);
        return NoContent();
    }

    [HttpGet("{id:int}/cors")]
    public async Task<IActionResult> Cors(int id)
    {
        var app = await appRepository.GetByAppId(id);
        return Ok(app.CorsSettings);
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

    [HttpGet("{id:int}/source-info")]
    public async Task<IActionResult> GetSourceInfo(int id)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Link)
            .Include(a => a.BuildConfigs)
            .SingleAsync(a => a.Id == id);

        if (app.Link is null)
        {
            return NotFound(new { Message = "No GitHub repository linked" });
        }

        // Use default branch if BuildConfigs or Branch is null/empty
        var branch = app.BuildConfigs?.Branch;
        if (string.IsNullOrEmpty(branch))
        {
            branch = AppBuildConfigs.Default.Branch;
        }

        try
        {
            var commitInfo = await githubAppService.GetLatestCommitByRepoId(
                app.Link.InstallationId,
                app.Link.RepoId,
                branch
            );

            return Ok(new
            {
                Branch = branch,
                Repository = commitInfo.RepositoryFullName,
                RepositoryUrl = $"https://github.com/{commitInfo.RepositoryFullName}",
                Commit = new
                {
                    Sha = commitInfo.Sha,
                    Message = commitInfo.Commit.Message,
                    Author = commitInfo.Commit.Author.Name,
                    Date = commitInfo.Commit.Author.Date,
                    Url = commitInfo.HtmlUrl
                }
            });
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new { Message = $"Failed to fetch commit info: {ex.Message}" });
        }
    }

    [HttpPost("{appId}/routing-config")]
    public async Task<IActionResult> UpdateRoutingConfig(int appId, [FromBody] UpdateRoutingConfigRequest request)
    {
        var config = new Api.Domain.Entities.RoutingConfig
        {
            SchemaVersion = request.SchemaVersion,
            Routes = request.Routes.Select(r => new RoutingConfigRoute
            {
                Name = r.Name,
                Priority = r.Priority,
                Match = new Api.Domain.Entities.RouteMatch
                {
                    Type = (Api.Domain.Entities.RouteMatchType)r.Match.Type,
                    Path = r.Match.Path
                },
                Target = new Api.Domain.Entities.RouteTarget
                {
                    Type = (Api.Domain.Entities.RouteTargetType)r.Target.Type,
                    StripPrefix = r.Target.StripPrefix,
                    Rewrite = r.Target.Rewrite,
                    Fallback = r.Target.Fallback
                }
            }).ToList()
        };

        await appService.SetRoutingConfig(appId, config);
        var app = await appRepository.GetByAppId(appId);
        await cacheInvalidator.InvalidateByIdAsync(appId);
        await specPublisher.PublishAsync(app.Slug);
        return NoContent();
    }

    [HttpGet("{appId}/routing-config")]
    public async Task<IActionResult> GetRoutingConfig(int appId)
    {
        var app = await appRepository.GetByAppId(appId);
        var config = app.RoutingConfig is { Routes.Count: > 0 }
            ? app.RoutingConfig
            : Api.Domain.Entities.RoutingConfig.Default;

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