﻿namespace WebApp.RestApi.Filters;

using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class GitHubWebhookValidationFilter : IAsyncActionFilter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubWebhookValidationFilter> _logger;

    public GitHubWebhookValidationFilter(IConfiguration configuration, ILogger<GitHubWebhookValidationFilter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Retrieve the secret token from configuration
        var secret = _configuration["GitHubWebhookSecret"];
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogError("GitHub Webhook secret is not configured.");
            context.Result = new BadRequestObjectResult("Server misconfiguration.");
            return;
        }

        // Get the signature from the request header
        var signatureHeader = context.HttpContext.Request.Headers["X-Hub-Signature-256"].ToString();
        if (string.IsNullOrEmpty(signatureHeader))
        {
            _logger.LogWarning("Missing GitHub signature header.");
            context.Result = new UnauthorizedObjectResult("Missing GitHub signature.");
            return;
        }

        // Read the request body
        context.HttpContext.Request.Body.Position = 0; // Reset the request body stream position for reading
        using var reader = new StreamReader(context.HttpContext.Request.Body);
        var requestBody = await reader.ReadToEndAsync();
        context.HttpContext.Request.Body.Position = 0; // Reset position again for the next middleware/action

        // Compute the HMAC SHA-256 hash of the payload using the secret
        var signature = ComputeHmacSha256Hash(requestBody, secret);

        // Compare the computed signature with the one sent by GitHub
        if (!VerifySignature(signature, signatureHeader))
        {
            _logger.LogWarning("GitHub signature verification failed.");
            context.Result = new UnauthorizedObjectResult("Invalid GitHub signature.");
            return;
        }

        // Proceed to the action method if the signature is valid
        await next();
    }

    // Compute HMAC SHA-256 hash
    private string ComputeHmacSha256Hash(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return $"sha256={BitConverter.ToString(hashBytes).Replace("-", "").ToLower()}";
    }

    // Verify that the computed hash matches the GitHub signature
    private bool VerifySignature(string computedSignature, string githubSignature)
    {
        return string.Equals(computedSignature, githubSignature, StringComparison.OrdinalIgnoreCase);
    }
}
