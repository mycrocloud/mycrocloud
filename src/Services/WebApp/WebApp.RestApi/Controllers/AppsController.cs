using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using WebApp.Domain.Services;
using WebApp.Infrastructure;
using WebApp.RestApi.Extensions;
using WebApp.RestApi.Filters;
using WebApp.RestApi.Models;
using WebApp.RestApi.Services;

namespace WebApp.RestApi.Controllers;

[TypeFilter<AppOwnerActionFilter>(Arguments = ["id"])]
public class AppsController(
    IAppService appService,
    IAppRepository appRepository,
    AppDbContext appDbContext,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IHostEnvironment environment,
    RabbitMqService rabbitMqService
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
        var app = Map(appCreateRequest);
        await appService.Create(User.GetUserId(), app);
        return Created(app.Id.ToString(), new
        {
            app.Id,
            app.CreatedAt,
            app.Version
        });

        App Map(AppCreateRequest source)
        {
            return new App
            {
                Name = source.Name,
                Description = source.Description,
                Status = AppStatus.Active,
                CorsSettings = CorsSettings.Default,
                Settings = AppSettings.Default,
                Version = Guid.NewGuid()
            };
        }
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
            app.GitHubRepoFullName,
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
    public async Task<IActionResult> ConnectGitHubRepo(int id, string repoFullName)
    {
        var userToken = await appDbContext.UserTokens
            .Where(t => t.UserId == User.GetUserId() && t.Provider == "GitHub" &&
                        t.Purpose == UserTokenPurpose.AppIntegration)
            .SingleOrDefaultAsync();

        if (userToken is null)
        {
            return Unauthorized();
        }

        var app = await appDbContext.Apps.SingleAsync(a => a.Id == id);

        // Fetch repo details
        var repo = await GetGitHubRepo(repoFullName, userToken);

        // Add webhook
        var webhookToken = Guid.NewGuid().ToString();
        await CreateWebhook(id, repo.FullName, userToken.Token, webhookToken);

        app.GitHubRepoFullName = repo.FullName;
        app.GitHubWebhookToken = webhookToken;
        await appDbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}/integrations/github")]
    public async Task<IActionResult> DisconnectGitHubRepo(int id)
    {
        var app = await appDbContext.Apps.SingleAsync(a => a.Id == id);

        app.GitHubRepoFullName = null;
        app.GitHubWebhookToken = null;

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

    private async Task CreateWebhook(int appId, string repoFullName, string accessToken,
        string webhookToken)
    {
        var config = configuration.GetSection("AppIntegrations:GitHubWebhook");

        //for testing webhook locally
        //ref: https://docs.github.com/en/webhooks/testing-and-troubleshooting-webhooks/testing-webhooks
        var url = string.Format(config["Config:Url"]!, appId, webhookToken);

        var webhookRequestBody = new
        {
            events = config.GetSection("Events").Get<string[]>(),
            config = new
            {
                url = url,
                content_type = "json",
                secret = config["Config:Secret"]
            }
        };
        var json = JsonSerializer.Serialize(webhookRequestBody);

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/repos/{repoFullName}/hooks");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("WebApp", "1.0"));
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("Authorization", $"Bearer {accessToken}");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        var content = new StringContent(json, null, "application/json");
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}