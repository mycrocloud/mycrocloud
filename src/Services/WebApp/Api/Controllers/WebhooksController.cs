using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Filters;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Models;
using Api.Domain.Entities;
using Api.Domain.Messages;
using Api.Infrastructure;

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
            // Fetch latest commit info from GitHub
            var config = app.BuildConfigs ?? AppBuildConfigs.Default;
            var branch = string.IsNullOrEmpty(config.Branch) ? AppBuildConfigs.Default.Branch : config.Branch;
            
            GitHubCommitInfo? commitInfo = null;
            try
            {
                commitInfo = await gitHubAppService.GetLatestCommitByRepoId(
                    app.Link!.InstallationId,
                    app.Link.RepoId,
                    branch
                );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch commit info for webhook build");
            }

            var build = new AppBuild
            {
                Id = Guid.NewGuid(),
                App = app,
                Status = AppBuildState.queued,
                CreatedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, string>()
            };
            
            // Populate metadata if commit info is available
            if (commitInfo != null)
            {
                if (!string.IsNullOrEmpty(commitInfo.Sha))
                    build.Metadata[BuildMetadataKeys.CommitSha] = commitInfo.Sha;
                if (!string.IsNullOrEmpty(commitInfo.Commit.Message))
                    build.Metadata[BuildMetadataKeys.CommitMessage] = commitInfo.Commit.Message;
                if (!string.IsNullOrEmpty(commitInfo.Commit.Author.Name))
                    build.Metadata[BuildMetadataKeys.Author] = commitInfo.Commit.Author.Name;
            }
            if (!string.IsNullOrEmpty(branch))
                build.Metadata[BuildMetadataKeys.Branch] = branch;

            appDbContext.AppBuildJobs.Add(build);

            // Create deployment immediately with Building status
            var deployment = new SpaDeployment
            {
                Id = Guid.NewGuid(),
                AppId = app.Id,
                BuildId = build.Id,
                ArtifactId = null,
                Status = DeploymentStatus.Building
            };
            appDbContext.SpaDeployments.Add(deployment);

            var buildConfig = app.BuildConfigs ?? AppBuildConfigs.Default;

            // TODO: Get limits based on user's subscription plan
            // var planLimits = await GetUserPlanLimits(app.UserId);
            var planLimits = PlanLimits.Free;

            var message = new AppBuildMessage
            {
                BuildId = build.Id.ToString(),
                RepoFullName = repoFullName,
                CloneUrl = cloneUrl.Replace("https://", "https://x-access-token:" + token + "@"),
                Branch = buildConfig.Branch,
                Directory = buildConfig.Directory,
                OutDir = buildConfig.OutDir,
                InstallCommand = buildConfig.InstallCommand,
                BuildCommand = buildConfig.BuildCommand,
                ArtifactsUploadPath = linkGenerator.GetPathByAction(HttpContext, nameof(BuildsController.UploadArtifacts), BuildsController.Controller, new { appId = app.Id, buildId = build.Id })!,
                Limits = planLimits
            };

            rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));
            
            publisher.Publish(app.Id, build.Status);

            await appDbContext.SaveChangesAsync();
        }

        return Ok();
    }
}