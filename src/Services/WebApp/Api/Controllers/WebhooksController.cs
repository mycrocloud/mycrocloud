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
public class WebhooksController(
    GitHubAppService gitHubAppService, 
    AppDbContext appDbContext, 
    BuildOrchestrationService buildOrchestrationService,
    ILogger<WebhooksController> logger) : ControllerBase
{
    // TODO: Store raw webhook events for debugging/replay
    // TODO: Deduplicate using X-GitHub-Delivery header
    // TODO: Queue webhook payloads to RabbitMQ for async processing at scale
    [HttpPost("github/postreceive")]
    [TypeFilter<GitHubWebhookValidationFilter>]
    public async Task<IActionResult> GitHubAppPostReceive()
    {
        var rawBodyString = HttpContext.Items["RawBodyString"] as string;
        var payloadNode = JsonNode.Parse(rawBodyString!);
        var eventType = Request.Headers["X-GitHub-Event"].ToString();

        return eventType switch
        {
            "push" => await HandlePushEvent(payloadNode!),
            "installation" => await HandleInstallationEvent(payloadNode!),
            "installation_repositories" => await HandleInstallationRepositoriesEvent(payloadNode!),
            _ => Ok()
        };
    }

    private async Task<IActionResult> HandlePushEvent(JsonNode payloadNode)
    {
        var installationId = (long)payloadNode["installation"]!["id"]!;
        var repoId = (long)payloadNode["repository"]!["id"]!;
        var repoFullName = (string)payloadNode["repository"]!["full_name"]!;
        var cloneUrl = (string)payloadNode["repository"]!["clone_url"]!;

        logger.LogInformation("Received GitHub push webhook for installation {InstallationId} and repository {RepoId}", installationId, repoId);

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

        var authenticatedCloneUrl = cloneUrl.Replace("https://", "https://x-access-token:" + token + "@");

        foreach (var app in apps)
        {
            var artifactsUploadPath = $"/apps/{app.Id}/builds/{{buildId}}/artifacts";

            await buildOrchestrationService.CreateAndQueueBuildAsync(
                app,
                authenticatedCloneUrl,
                repoFullName,
                artifactsUploadPath
            );
        }

        return Ok();
    }

    private async Task<IActionResult> HandleInstallationEvent(JsonNode payloadNode)
    {
        var action = (string)payloadNode["action"]!;
        var installationId = (long)payloadNode["installation"]!["id"]!;

        logger.LogInformation("Received GitHub installation webhook: action={Action}, installationId={InstallationId}", action, installationId);

        if (action == "deleted")
        {
            var installation = await appDbContext.GitHubInstallations
                .Include(i => i.AppLinks)
                .SingleOrDefaultAsync(i => i.InstallationId == installationId);

            if (installation is null)
            {
                logger.LogWarning("Received installation deleted webhook for unknown installation {InstallationId}", installationId);
                return Ok();
            }

            if (installation.AppLinks?.Count > 0)
            {
                appDbContext.RemoveRange(installation.AppLinks);
            }

            appDbContext.GitHubInstallations.Remove(installation);
            await appDbContext.SaveChangesAsync();

            logger.LogInformation("Deleted GitHub installation {InstallationId} and {LinkCount} associated app links", installationId, installation.AppLinks?.Count ?? 0);
        }

        return Ok();
    }

    private async Task<IActionResult> HandleInstallationRepositoriesEvent(JsonNode payloadNode)
    {
        var action = (string)payloadNode["action"]!;
        var installationId = (long)payloadNode["installation"]!["id"]!;

        logger.LogInformation("Received GitHub installation_repositories webhook: action={Action}, installationId={InstallationId}", action, installationId);

        if (action == "removed")
        {
            var removedRepos = payloadNode["repositories_removed"]!.AsArray();
            var removedRepoIds = removedRepos.Select(r => (long)r!["id"]!).ToList();

            var appLinks = await appDbContext.Set<AppLink>()
                .Where(l => l.InstallationId == installationId && removedRepoIds.Contains(l.RepoId))
                .ToListAsync();

            if (appLinks.Count > 0)
            {
                appDbContext.RemoveRange(appLinks);
                await appDbContext.SaveChangesAsync();

                logger.LogInformation("Removed {Count} app links for repos removed from installation {InstallationId}", appLinks.Count, installationId);
            }
        }

        return Ok();
    }
}