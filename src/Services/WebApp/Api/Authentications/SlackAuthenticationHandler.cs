using System.Security.Claims;
using System.Text.Encodings.Web;
using Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Api.Authentications;

public class SlackAuthenticationHandler: AuthenticationHandler<SlackAuthenticationOptions>
{
    private readonly SlackAppService _slackAppService;
    private readonly ILogger<SlackAuthenticationHandler> _logger;

    public SlackAuthenticationHandler(IOptionsMonitor<SlackAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, SlackAppService slackAppService) : base(options, logger, encoder, clock)
    {
        _slackAppService = slackAppService;
        _logger = logger.CreateLogger<SlackAuthenticationHandler>();
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        _logger.LogTrace("HandleAuthenticateAsync");
        var body = Context.Items["Slack:Body"] as string;

        var query = QueryHelpers.ParseQuery(body);
        
        var teamId = query["team_id"];
        var slackUserId = query["user_id"];

        var userId = await _slackAppService.FindUserId(teamId, slackUserId);

        if (userId is null)
        {
            return AuthenticateResult.Fail("Invalid token");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("SlackTeamId", teamId),
            new("SlackUserId", slackUserId),
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        
        var claimPrincipal = new ClaimsPrincipal(identity);
        
        var ticket = new AuthenticationTicket(claimPrincipal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status200OK;
        
        await Response.WriteAsJsonAsync(new
        {
            response_type = "ephemeral",
            text = "You are not signed in to MycroCloud. Use `/mycrocloud signin` to to sign in."
        });
    }
}

public class SlackAuthenticationOptions : AuthenticationSchemeOptions
{
}