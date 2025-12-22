using System.Security.Claims;

namespace Storages.Api.Helpers;

public static class IdentityExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal) {
        return principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
    }
}