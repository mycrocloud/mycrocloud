using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.MiniApiGateway.Middlewares;

public class AuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var app = (App)context.Items["_App"]!;
        var appRepository = context.RequestServices.GetService<IAppRepository>()!;
        var authenticationSchemes = await appRepository.GetAuthenticationSchemes(app.Id);
        if (!authenticationSchemes.Any())
        {
            await next.Invoke(context);
            return;
        }
        foreach (var scheme in authenticationSchemes)
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

    private static async Task AuthenticateOpenIdConnectScheme(HttpContext context, AuthenticationScheme scheme)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
        {
            return;
        }
        var cachedOpenIdConnectionSigningKeys = context.RequestServices.GetService<ICachedOpenIdConnectionSigningKeys>()!;
        var signingKeys = await cachedOpenIdConnectionSigningKeys.Get(scheme.OpenIdConnectAuthority);
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

    private static async Task AuthenticateApiKeyScheme(HttpContext context, App app, AuthenticationScheme scheme)
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

    private static bool ValidateToken(string token, 
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
        catch
        {
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