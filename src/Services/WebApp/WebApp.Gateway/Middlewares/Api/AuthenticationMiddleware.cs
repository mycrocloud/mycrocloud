using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Api.Domain.Enums;
using Api.Domain.Models;
using Api.Infrastructure;

namespace WebApp.Gateway.Middlewares.Api;

public class AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var app = (AppSpecification)context.Items["_AppSpecification"]!;

        if (app.AuthenticationSchemes.Count == 0)
        {
            await next.Invoke(context);
            return;
        }

        foreach (var scheme in app.AuthenticationSchemes)
        {
            switch (scheme.Type)
            {
                case AuthenticationSchemeType.OpenIdConnect:
                {
                    await AuthenticateOpenIdConnectScheme(context, scheme);
                    break;
                }
                case AuthenticationSchemeType.ApiKey:
                {
                    await AuthenticateApiKeyScheme(context, app, scheme);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        await next.Invoke(context);
    }

    private async Task AuthenticateOpenIdConnectScheme(HttpContext context, CachedAuthenticationScheme scheme)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }
        var cachedOpenIdConnectionSigningKeys = context.RequestServices.GetService<ICachedOpenIdConnectionSigningKeys>()!;
        var signingKeys = await cachedOpenIdConnectionSigningKeys.Get(scheme.OpenIdConnectAuthority.TrimEnd('/'));
        if (!ValidateToken(token, scheme.OpenIdConnectAuthority,
                scheme.OpenIdConnectAudience,
                signingKeys, out var jwt))
        {
            return;
        }
        ArgumentNullException.ThrowIfNull(jwt);
        //TODO: 
        var claims = jwt.Claims;
        var user = new Dictionary<string, string>();
        foreach (var claim in claims)
        {
            //TODO: A claim can have multiple values
            if (!user.TryAdd(claim.Type, claim.Value))
            {
                user[claim.Type] = claim.Value;
            }
        }
        context.Items.Add("_AuthenticatedScheme", scheme);
        context.Items.Add("_OpenIdConnectUser", user);
    }

    private static async Task AuthenticateApiKeyScheme(HttpContext context, AppSpecification app, CachedAuthenticationScheme scheme)
    {
        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            return;
        }
        var appDbContext = context.RequestServices.GetService<AppDbContext>()!;
        var apiKeyEntity = await appDbContext.ApiKeys
            .Where(k => k.App.Id == app.Id && k.Key == apiKey)
            .SingleOrDefaultAsync();
        if (apiKeyEntity is null)
        {
            return;
        }
        context.Items.Add("_AuthenticatedScheme", scheme);
        context.Items.Add("_ApiKey", apiKeyEntity);
    }

    private bool ValidateToken(string token, 
        string issuer, 
        string audience, 
        IEnumerable<SecurityKey> signingKeys,
        out JwtSecurityToken? jwt)
    {
        var validationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true
        };
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            jwt = (JwtSecurityToken)validatedToken;

            return true;
        }
        catch(Exception e)
        {
            logger.LogDebug(e, "Token validation failed");
            jwt = null;
            return false;
        }
    }
}
public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}