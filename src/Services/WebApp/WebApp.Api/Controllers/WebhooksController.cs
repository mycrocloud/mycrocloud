using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Api.Models;
using WebApp.Api.Services;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Controllers;

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
            var job = new AppBuildJob
            {
                Id = Guid.NewGuid(),
                App = app,
                Name = commitMessage ?? $"Build {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                Status = "Queued",
                CreatedAt = DateTime.UtcNow
            };

            appDbContext.AppBuildJobs.Add(job);

            var config = app.BuildConfigs;
            
            var message = new AppBuildMessage
            {
                JobId = job.Id.ToString(),
                RepoFullName = repoFullName,
                CloneUrl = cloneUrl.Replace("https://", "https://x-access-token:" + token + "@"),
                Branch = config.Branch,
                Directory = config.Directory,
                OutDir = config.OutDir,
                InstallCommand = config.InstallCommand,
                BuildCommand = config.BuildCommand,
                ArtifactsUploadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(ObjectsController.PutObject), ObjectsController.Controller, new { appId = app.Id })!
            };

            rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));
            
            publisher.Publish(app.Id, job.Status);

            await appDbContext.SaveChangesAsync();
        }

        return Ok();
    }
}