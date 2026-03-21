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
    AppDbContext appDbContext,
    IServiceScopeFactory scopeFactory,
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
            "push" => HandlePushEvent(payloadNode!),
            "installation" => await HandleInstallationEvent(payloadNode!),
            "installation_repositories" => await HandleInstallationRepositoriesEvent(payloadNode!),
            _ => Ok()
        };
    }

    private IActionResult HandlePushEvent(JsonNode payloadNode)
    {
        var installationId = (long)payloadNode["installation"]!["id"]!;
        var repoId = (long)payloadNode["repository"]!["id"]!;
        var repoFullName = (string)payloadNode["repository"]!["full_name"]!;
        var cloneUrl = (string)payloadNode["repository"]!["clone_url"]!;

        logger.LogInformation("Received GitHub push webhook for installation {InstallationId} and repository {RepoId}", installationId, repoId);

        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ghSvc = scope.ServiceProvider.GetRequiredService<GitHubAppService>();
            var orchSvc = scope.ServiceProvider.GetRequiredService<BuildOrchestrationService>();
            var bgLogger = scope.ServiceProvider.GetRequiredService<ILogger<WebhooksController>>();

            try
            {
                var installation = await db.GitHubInstallations
                    .SingleOrDefaultAsync(i => i.InstallationId == installationId);

                if (installation is null)
                {
                    bgLogger.LogWarning("Installation {InstallationId} not found, skipping push event", installationId);
                    return;
                }

                var token = await ghSvc.GetInstallationAccessToken(installation.InstallationId);
                var authenticatedCloneUrl = cloneUrl.Replace("https://", "https://x-access-token:" + token + "@");

                var apps = await db.Apps
                    .Include(a => a.Link)
                    .Where(a => a.Link != null &&
                                a.Link.InstallationId == installationId &&
                                a.Link.RepoId == repoId)
                    .ToListAsync();

                foreach (var app in apps)
                {
                    var build = await orchSvc.CreateBuildAsync(app);
                    try
                    {
                        await orchSvc.QueueBuildAsync(build.Id, app.Id, authenticatedCloneUrl, repoFullName);
                    }
                    catch (Exception ex)
                    {
                        bgLogger.LogError(ex, "Failed to queue build {BuildId}", build.Id);
                        await orchSvc.MarkBuildFailedAsync(build.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                bgLogger.LogError(ex, "Failed to process push event for installation {InstallationId}", installationId);
            }
        });

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
