using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Api.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using WebApp.Domain.Services;
using WebApp.Infrastructure;
using WebApp.Api.Extensions;
using WebApp.Api.Services;

namespace WebApp.Api.Controllers;

[TypeFilter<AppOwnerActionFilter>(Arguments = ["id"])]
public class AppsController(
    IAppService appService,
    IAppRepository appRepository,
    AppDbContext appDbContext,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    LinkGenerator linkGenerator,
    GitHubAppService githubAppService
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
            Integration = app.Integration != null
                ? new
                {
                    app.Integration.InstallationId,
                    app.Integration.RepoId,
                }
                : null,
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

        await appService.Rename(id, renameRequest.Name);
        return NoContent();
    }

    [HttpPatch("{id:int}/Status")]
    public async Task<IActionResult> SetStatus(int id, AppStatus status)
    {
        await appService.SetStatus(id, status);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await appService.Delete(id);
        return NoContent();
    }

    [HttpPatch("{id:int}/cors")]
    public async Task<IActionResult> Cors(int id, CorsSettings settings)
    {
        await appService.SetCorsSettings(id, settings);
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
            .Include(a => a.Integration)
            .ThenInclude(i => i.GitHubInstallation)
            .SingleAsync(a => a.Id == id && a.UserId == User.GetUserId());

        if (app.Integration is not { } integration)
        {
            return NotFound();
        }
        
        return Ok(new
        {
            Type = "GitHub", //TODO: 
            Org = integration.GitHubInstallation.AccountLogin, 
            integration.RepoId,
            Repo = integration.RepoName
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
            .Include(a => a.Integration)
            .SingleAsync(a => a.Id == id);
        
        app.Integration = new AppIntegration
        {
            InstallationId = installation.InstallationId,
            RepoId = repo.Id,
            RepoName = repo.Name,
            Branch = "main",
            Directory = "/",
            BuildCommand = "npm install && npm run build",
            OutDir = "dist",
            InstallCommand = "npm install"
        };
        
        await appDbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}/link")]
    public async Task<IActionResult> DisconnectGitHubRepo(int id)
    {
        var app = await appDbContext.Apps
            .Include(app => app.Integration)
            .SingleAsync(a => a.Id == id);

        appDbContext.Remove(app.Integration);

        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }
}

public class GitHubRepoIntegrationRequest
{
    public long InstallationId { get; set; }
    public long RepoId { get; set; }
}