using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Filters;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Messages;
using WebApp.Infrastructure;

namespace Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WebhooksController(GitHubAppService gitHubAppService, 
    AppDbContext appDbContext, 
    RabbitMqService rabbitMqService,
    IAppBuildPublisher publisher,
    LinkGenerator linkGenerator,
    ILogger<WebhooksController> logger) : ControllerBase
{
    [HttpPost("github/postreceive")]
    [TypeFilter<GitHubWebhookValidationFilter>]
    public async Task<IActionResult> GitHubAppPostReceive()
    {
        var rawBodyString = HttpContext.Items["RawBodyString"] as string;
        var payloadNode = JsonNode.Parse(rawBodyString!);
        var installationId = (long)payloadNode!["installation"]!["id"]!;
        var repoId = (long)payloadNode["repository"]!["id"]!;
        var repoFullName = (string)payloadNode["repository"]!["full_name"]!;
        var cloneUrl = (string)payloadNode["repository"]!["clone_url"]!;
        var commitMessage = (string?)payloadNode["head_commit"]?["message"];

        logger.LogInformation("Received GitHub App webhook for installation {InstallationId} and repository {RepoId}", installationId, repoId);
        
        var installation = await appDbContext.GitHubInstallations
            .SingleOrDefaultAsync(i => i.InstallationId == installationId);

        if (installation is null)
        {
            return BadRequest();
        }
        
        var token = await gitHubAppService.GetInstallationAccessToken(installation.InstallationId);
        
        var apps = await appDbContext.Apps
            .Include(a => a.Link)
            .Where(a => a.Link != null &&
                        a.Link.InstallationId == installationId &&
                        a.Link.RepoId == repoId)
            .ToListAsync();
        
        foreach (var app in apps)
        {
            var build = new AppBuild
            {
                Id = Guid.NewGuid(),
                App = app,
                Name = commitMessage ?? $"Build {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                Status = "Queued",
                CreatedAt = DateTime.UtcNow
            };

            appDbContext.AppBuildJobs.Add(build);

            var config = app.BuildConfigs;
            
            var message = new AppBuildMessage
            {
                BuildId = build.Id.ToString(),
                RepoFullName = repoFullName,
                CloneUrl = cloneUrl.Replace("https://", "https://x-access-token:" + token + "@"),
                Branch = config.Branch,
                Directory = config.Directory,
                OutDir = config.OutDir,
                InstallCommand = config.InstallCommand,
                BuildCommand = config.BuildCommand,
                ArtifactsUploadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(BuildsController.PutObject), BuildsController.Controller, new { appId = app.Id, buildId = build.Id })!
            };

            rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));
            
            publisher.Publish(app.Id, build.Status);

            await appDbContext.SaveChangesAsync();
        }

        return Ok();
    }
}