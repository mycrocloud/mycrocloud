using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Authentications;

public class ApiTokenAuthenticationHandler: AuthenticationHandler<ApiTokenAuthenticationOptions>
{
    private readonly AppDbContext _dbContext;

    public ApiTokenAuthenticationHandler(IOptionsMonitor<ApiTokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, AppDbContext dbContext) : base(options, logger, encoder, clock)
    {
        _dbContext = dbContext;
    }

    public ApiTokenAuthenticationHandler(IOptionsMonitor<ApiTokenAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = Context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        var userToken = await _dbContext.UserTokens
            .SingleOrDefaultAsync(t =>
                t.Token == token && t.Purpose == UserTokenPurpose.ApiToken && t.Status != TokenStatus.Revoked);

        if (userToken is null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userToken.UserId)
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        
        var claimPrincipal = new ClaimsPrincipal(identity);
        
        var ticket = new AuthenticationTicket(claimPrincipal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}