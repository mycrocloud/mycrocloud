using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.Services;

public class GitHubAppService(HttpClient httpClient, IOptions<GitHubAppOptions> options)
{
    private readonly GitHubAppOptions _options = options.Value;

    private string GenerateJwt()
    {
        var now = DateTimeOffset.UtcNow;
        var securityKey = new RsaSecurityKey(ReadPrivateKey(_options.PrivateKeyPath));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.AppId,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(10).UtcDateTime,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RSA ReadPrivateKey(string path)
    {
        var privateKeyPem = File.ReadAllText(path);
        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        return rsa;
    }

    public async Task<JsonElement> GetInstallation(long installationId)
    {
        var jwt = GenerateJwt();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var json = await httpClient.GetStringAsync($"https://api.github.com/app/installations/{installationId}");
        var doc = JsonDocument.Parse(json).RootElement;

        return doc;
    }

    public async Task<string> GetInstallationAccessToken(long installationId)
    {
        var jwt = GenerateJwt();

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        var response =
            await httpClient.PostAsync($"https://api.github.com/app/installations/{installationId}/access_tokens",
                null);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    public async Task<List<GitHubRepo>> GetAccessibleRepos(long installationId, string? token = null)
    {
        token ??= await GetInstallationAccessToken(installationId);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var json = await httpClient.GetStringAsync("https://api.github.com/installation/repositories");

        var node = JsonNode.Parse(json)!;
        var repos = JsonSerializer.Deserialize<List<GitHubRepo>>(node["repositories"]!.ToJsonString())!;

        return repos;
    }
}

public class GitHubAppInstallation
{
    [JsonPropertyName("installation_id")] public long InstallationId { get; set; }

    [JsonPropertyName("setup_action")] public string SetupAction { get; set; }
}

public class GitHubAppOptions
{
    public string AppId { get; set; } = "";
    public string PrivateKeyPath { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
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