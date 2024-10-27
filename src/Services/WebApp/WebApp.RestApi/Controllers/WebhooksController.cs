using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;
using WebApp.RestApi.Filters;
using WebApp.RestApi.Models;
using WebApp.RestApi.Services;

namespace WebApp.RestApi.Controllers;

public partial class WebhooksController(AppDbContext appDbContext, RabbitMqService rabbitMqService) : BaseController
{
    [HttpPost("github/postreceive/{appId:int}")]
    [AllowAnonymous]
    [TypeFilter<GitHubWebhookValidationFilter>]
    public async Task<IActionResult> ReceiveGitHubEvent(int appId, string token)
    {
        var rawBodyString = HttpContext.Items["RawBodyString"] as string;
        var payloadNode = JsonNode.Parse(rawBodyString!);
        var repoFullName = (string)payloadNode!["repository"]!["full_name"]!;

        var app = await appDbContext.Apps.SingleOrDefaultAsync(a => a.Id == appId && a.GitHubWebhookToken == token);
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
            Id = Guid.NewGuid().ToString(),
            App = app,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };
        
        appDbContext.AppBuildJobs.Add(job);
        
        var message = new AppBuildMessage
        {
            Id = job.Id,
            RepoFullName = repoFullName,
            CloneUrl = $"https://{userToken.Token}@github.com/{repoFullName}.git"
        };

        rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));
        
        await appDbContext.SaveChangesAsync();
        
        return Ok();
    }
}