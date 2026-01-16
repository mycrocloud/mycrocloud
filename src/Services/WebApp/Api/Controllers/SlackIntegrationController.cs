using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Api.Extensions;

namespace Api.Controllers;

[ApiController]
[Route("slack/integration")]
[IgnoreAntiforgeryToken]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SlackIntegrationController(IConfiguration configuration, SlackAppService slackAppService) : ControllerBase
{
    public const string ControllerName = "SlackIntegration";
    
    [HttpPost("link-callback")]
    public async Task<IActionResult> Link(LinkCallbackPayload payload)
    {
        var userId = User.GetUserId();
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Slack:LinkSecret"]);

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        var principal = tokenHandler.ValidateToken(payload.State, validationParams, out var _);
        var slackUserId = principal.FindFirstValue("slack_user_id");
        var slackTeamId = principal.FindFirstValue("slack_team_id");
        var channelId = principal.FindFirstValue("channel_id");

        if (string.IsNullOrEmpty(slackUserId) || string.IsNullOrEmpty(slackTeamId))
            return BadRequest("Invalid state payload");

        await slackAppService.LinkSlackUser(slackUserId, slackTeamId, userId);
        
        await slackAppService.SendSlackEphemeralMessage(slackTeamId, channelId!, slackUserId, $"✅ You are signed in to MycroCloud as {userId}");
        
        return Ok(new
        {
            redirect_url = $"https://slack.com/app_redirect?channel={channelId}&team={slackTeamId}"
        });
    }
}

public class LinkCallbackPayload
{
    public string State { get; set; }
}