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
}