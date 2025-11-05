using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Api.Models;
using WebApp.Api.Services;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Controllers;

public class WebhooksController(AppDbContext appDbContext, RabbitMqService rabbitMqService) : BaseController
{
    public const string ControllerName = "Webhooks";
    
    [HttpPost("github/postreceive/{appId:int}")]
    [AllowAnonymous]
    [TypeFilter<GitHubWebhookValidationFilter>]
    public async Task<IActionResult> ReceiveGitHubEvent(int appId, string token)
    {
        var rawBodyString = HttpContext.Items["RawBodyString"] as string;
        var payloadNode = JsonNode.Parse(rawBodyString!);
        var repoFullName = (string)payloadNode!["repository"]!["full_name"]!;
        var commitMessage = (string?)payloadNode["head_commit"]?["message"];

        var app = await appDbContext.Apps
            .Include(a => a.Integration)
            .SingleOrDefaultAsync(a => a.Id == appId && a.GitHubWebhookToken == token);

        if (app is null || repoFullName != app.GitHubRepoFullName)
        {
            return BadRequest();
        }

        var userToken = await appDbContext.UserTokens.SingleOrDefaultAsync(t => t.UserId == app.UserId
            && t.Provider == "GitHub" &&
            t.Purpose == UserTokenPurpose.AppIntegration
        );

        if (userToken is null)
        {
            return BadRequest();
        }

        var job = new AppBuildJob
        {
            Id = Guid.NewGuid(),
            App = app,
            Name = commitMessage ?? $"Build {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
            Status = "Queued",
            CreatedAt = DateTime.UtcNow
        };

        appDbContext.AppBuildJobs.Add(job);

        var message = new AppBuildMessage
        {
            JobId = job.Id.ToString(),
            RepoFullName = repoFullName,
            CloneUrl = $"https://{userToken.Token}@github.com/{repoFullName}.git",
            Directory = app.Integration?.Directory ?? ".",
            OutDir = app.Integration?.OutDir ?? "dist",
            InstallCommand = app.Integration?.InstallCommand ?? "npm install",
            BuildCommand = app.Integration?.BuildCommand ?? "npm run build"
        };

        rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));

        await appDbContext.SaveChangesAsync();

        return Ok();
    }
}