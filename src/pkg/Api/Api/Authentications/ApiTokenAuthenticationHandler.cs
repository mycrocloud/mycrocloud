using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Api.Domain.Entities;
using Api.Infrastructure;

namespace Api.Authentications;

public class ApiTokenAuthenticationHandler: AuthenticationHandler<ApiTokenAuthenticationOptions>
{
    private readonly AppDbContext _dbContext;

    public ApiTokenAuthenticationHandler(IOptionsMonitor<ApiTokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, AppDbContext dbContext) : base(options, logger, encoder)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        var hashedToken = TokenUtils.HashToken(token);
        var apiToken = await _dbContext.ApiTokens
            .SingleOrDefaultAsync(t => t.HashedToken == hashedToken && t.Status == TokenStatus.Active);

        if (apiToken is null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiToken.UserId)
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        
        var claimPrincipal = new ClaimsPrincipal(identity);
        
        var ticket = new AuthenticationTicket(claimPrincipal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}