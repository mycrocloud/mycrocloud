using System.Security.Claims;

namespace Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal) {
        return principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
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
