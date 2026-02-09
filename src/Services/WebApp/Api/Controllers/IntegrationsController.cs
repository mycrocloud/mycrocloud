using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Infrastructure;
using Api.Extensions;

namespace Api.Controllers;

[Route("[controller]")]
public class IntegrationsController(
    AppDbContext appDbContext,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    GitHubAppService githubService) : BaseController
{
    #region GitHub
    
    [HttpPost("github/callback")]
    public async Task<IActionResult> GitHubCallback(GitHubAppInstallation request)
    {
        var doc = await githubService.GetInstallation(request.InstallationId);

        var installation = await appDbContext.GitHubInstallations
            .SingleOrDefaultAsync(i => i.InstallationId == request.InstallationId);

        if (installation == null)
        {
            installation = new GitHubInstallation
            {
                InstallationId = request.InstallationId,
                AccountId = doc.GetProperty("account").GetProperty("id").GetInt64(),
                AccountLogin = doc.GetProperty("account").GetProperty("login").GetString()!,
                AccountType = Enum.Parse<GitHubAccountType>(doc.GetProperty("account").GetProperty("type").GetString()!),
                UserId = User.GetUserId(),
                CreatedAt = DateTime.UtcNow
            };
            
            appDbContext.GitHubInstallations.Add(installation);
        }
        else
        {
            installation.UpdatedAt = DateTime.UtcNow;
        }

        await appDbContext.SaveChangesAsync();

        return Ok();
    }
    
    [HttpGet("github/installations")]
    public async Task<IActionResult> GetInstallations()
    {
        var installations = await appDbContext.GitHubInstallations
            .Where(i => i.UserId == User.GetUserId())
            .ToListAsync();

        return Ok(installations.Select(i => new
        {
            i.InstallationId,
            i.AccountId,
            i.AccountLogin,
            AccountType = i.AccountType.ToString(),
            i.CreatedAt,
            i.UpdatedAt
        }));
    }

    [HttpGet("github/installations/{installationId:long}/repos")]
    public async Task<IActionResult> GetGitHubRepos(long installationId)
    {
        var installation = await appDbContext.GitHubInstallations
            .SingleAsync(i => i.InstallationId == installationId && i.UserId == User.GetUserId());

        var repos = await githubService.GetAccessibleRepos(installation.InstallationId);
        
        return Ok(repos.Select(repo => new
        {
            repo.Id,
            repo.Name,
            repo.FullName,
            repo.Description,
            repo.CreatedAt,
            repo.UpdatedAt
        }));
    }
    
    #endregion

    #region Slack

    [HttpPost("slack/callback")]
    public async Task<IActionResult> SlackCallback(OAuthRequest request)
    {
        var slack = await ExchangeSlackAccessToken(request);

        var existing = await appDbContext.SlackInstallations
        .FirstOrDefaultAsync(x => x.TeamId == slack.Team!.Id);

        if (existing == null)
        {
            var install = new SlackInstallation
            {
                //AppId = slack.AppId,
                TeamId = slack.Team!.Id,
                TeamName = slack.Team!.Name,
                BotUserId = slack.BotUserId,
                BotAccessToken = slack.AccessToken ?? "",
                Scopes = slack.Scope,
                InstalledByUserId = slack.AuthedUser?.Id,
                EnterpriseId = slack.Enterprise?.Id,
                IsEnterpriseInstall = slack.IsEnterpriseInstall,
                InstalledAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            appDbContext.SlackInstallations.Add(install);
        }
        else
        {
            //existing.AppId = slack.AppId;
            existing.TeamName = slack.Team!.Name;
            existing.BotUserId = slack.BotUserId;
            existing.BotAccessToken = slack.AccessToken ?? "";
            existing.Scopes = slack.Scope;
            existing.InstalledByUserId = slack.AuthedUser?.Id;
            existing.EnterpriseId = slack.Enterprise?.Id;
            existing.IsEnterpriseInstall = slack.IsEnterpriseInstall;
            existing.UpdatedAt = DateTime.UtcNow;

            appDbContext.SlackInstallations.Update(existing);
        }

        await appDbContext.SaveChangesAsync();

        return Ok();
    }

    private async Task<SlackOAuthResponse> ExchangeSlackAccessToken(OAuthRequest request)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("multipart/form-data"));
        var requestData = new Dictionary<string, string>
        {
            { "client_id", configuration[$"OAuthApps:Slack:ClientId"]! },
            { "client_secret", configuration[$"OAuthApps:Slack:ClientSecret"]! },
            { "code", request.Code },
            { "redirect_uri", request.RedirectUrl}
        };
        var response = await client.PostAsync("https://slack.com/api/oauth.v2.access",
            new FormUrlEncodedContent(requestData));
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();

        var authResponse = JsonSerializer.Deserialize<SlackOAuthResponse>(responseBody)!;
        return authResponse;
    }

    #endregion
}


public class OAuthRequest
{
    public string Code { get; set; }

    [JsonPropertyName("redirect_uri")] public string? RedirectUrl { get; set; }
}

public class SlackOAuthResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    [JsonPropertyName("authed_user")]
    public SlackAuthedUser? AuthedUser { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("bot_user_id")]
    public string? BotUserId { get; set; }

    [JsonPropertyName("team")]
    public SlackTeam? Team { get; set; }

    [JsonPropertyName("enterprise")]
    public SlackEnterprise? Enterprise { get; set; }

    [JsonPropertyName("is_enterprise_install")]
    public bool IsEnterpriseInstall { get; set; }
}

public class SlackAuthedUser
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}

public class SlackTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class SlackEnterprise
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}