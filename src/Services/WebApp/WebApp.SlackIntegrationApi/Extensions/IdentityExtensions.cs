using System.Security.Claims;

namespace WebApp.SlackIntegrationApi.Extensions;

public static class IdentityExtensions
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
    public static string GetSlackTeamId(this ClaimsPrincipal user)
    {
        return user.Claims.FirstOrDefault(c => c.Type == "SlackTeamId")?.Value;
    }
    
    public static string GetSlackUserId(this ClaimsPrincipal user)
    {
        return user.Claims.FirstOrDefault(c => c.Type == "SlackUserId")?.Value;
    }
}