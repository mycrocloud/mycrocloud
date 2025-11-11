using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;
using WebApp.Api.Extensions;
using WebApp.Api.Services;

namespace WebApp.Api.Controllers;

[Route("[controller]")]
public class IntegrationsController(
    AppDbContext appDbContext,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    GitHubAppService _githubService) : BaseController
{
    [HttpPost("github/callback")]
    public async Task<IActionResult> GitHubCallback(GitHubAppInstallation installation)
    {
        var repoJson = await ExchangeGitHubAccessToken(installation);

        return Ok(repoJson); // TODO: return OK(string) is good?
    }

    private async Task<string> ExchangeGitHubAccessToken(GitHubAppInstallation installation)
    {
        var installationId = installation.InstallationId;
        var client = httpClientFactory.CreateClient();
        var token = await _githubService.GetInstallationTokenAsync(installationId);
        
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MycroCloud", "1.0"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        
        var reposJson = await client.GetStringAsync("https://api.github.com/installation/repositories");
        
        return reposJson;
    }

    [HttpGet("github/repos")]
    public async Task<IActionResult> GetGitHubRepos()
    {
        var userToken = await appDbContext.UserTokens
            .Where(t => t.UserId == User.GetUserId() && t.Provider == "GitHub" &&
                        t.Purpose == UserTokenPurpose.AppIntegration)
            .SingleOrDefaultAsync();

        if (userToken is null)
        {
            return Unauthorized();
        }

        var client = httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/repos");
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("WebApp", "1.0"));
        request.Headers.Add("Accept", "application/vnd.github+json");
        request.Headers.Add("Authorization", "Bearer " + userToken.Token);
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return Unauthorized();
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var repos = JsonSerializer.Deserialize<List<GitHubRepo>>(responseBody)!;
        return Ok(repos.Select(repo => new
        {
            repo.Name,
            repo.FullName,
            repo.Description,
            repo.CreatedAt,
            repo.UpdatedAt
        }));
    }

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

public class GitHubRepo
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("full_name")] public string FullName { get; set; }

    [JsonPropertyName("description")] public string Description { get; }

    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
}

public class GitHubAppInstallation
{
    [JsonPropertyName("installation_id")] public long InstallationId { get; set; }

    [JsonPropertyName("setup_action")] public string SetupAction { get; set; }
}

public class OAuthRequest
{
    public string Code { get; set; }

    [JsonPropertyName("redirect_uri")] public string? RedirectUrl { get; set; }
}

public class OAuthResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; }
}

// public class SlackOAuthResponse : OAuthResponse
// {

// }

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