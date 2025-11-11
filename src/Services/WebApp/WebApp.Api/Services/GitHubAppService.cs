using System.Security.Claims;

namespace WebApp.Api.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Text.Json;

public class GitHubAppService(HttpClient httpClient, IOptions<GitHubAppOptions> options)
{
    private readonly GitHubAppOptions _options = options.Value;

    public string GenerateJwt()
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

    public async Task<string> GetInstallationTokenAsync(long installationId)
    {
        var jwt = GenerateJwt();
        
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MycroCloud", "1.0"));
        
        var url = $"https://api.github.com/app/installations/{installationId}/access_tokens";
        var response = await httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    public async Task<string> GetInstallationRepos(long installationId)
    {
        var jwt = GenerateJwt();
        
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MycroCloud", "1.0"));
        
        var url = $"https://api.github.com/app/installations/{installationId}/repositories";
        var response = await httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }
}

public class GitHubAppOptions
{
    public string AppId { get; set; } = "";
    public string PrivateKeyPath { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
}