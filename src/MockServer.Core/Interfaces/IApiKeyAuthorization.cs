using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MockServer.Core.Interfaces;
public class ApiKeyAuthorizationOptions {

}
public interface IApiKeyAuthorization {
    string GenerateKey(ApiKeyAuthorizationOptions options);
    ClaimsPrincipal Validate(string key, ApiKeyAuthorizationOptions options);
}