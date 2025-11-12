using System.Net.Http.Headers;
using System.Text.Json;
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

    [HttpPost("{id:int}/integrations/github")]
    public async Task<IActionResult> ConnectGitHubRepo(int id, long installationId, long repoId)
    {
        var installation = await appDbContext.GitHubInstallations
            .Where(i => i.InstallationId == installationId && i.UserId == User.GetUserId())
            .SingleAsync();

        var repos = await githubAppService.GetAccessibleRepos(installation.InstallationId);
        
        var repo = repos.Single(r => r.Id == repoId);

        var app = await appDbContext.Apps.SingleAsync(a => a.Id == id);
        
        app.Integration = new AppIntegration
        {
            InstallationId = installation.InstallationId,
            RepoId = repoId,
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

    [HttpDelete("{id:int}/integrations/github")]
    public async Task<IActionResult> DisconnectGitHubRepo(int id)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == id);

        app.Integration = null;

        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }

    private async Task<GitHubRepo> GetGitHubRepo(string repoFullName, UserToken userToken)
    {
        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repoFullName}");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("WebApp", "1.0"));
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("Authorization", "Bearer " + userToken.Token);
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var repo = JsonSerializer.Deserialize<GitHubRepo>(responseBody)!;
        return repo;
    }
}