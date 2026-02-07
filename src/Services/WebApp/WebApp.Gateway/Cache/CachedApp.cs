using WebApp.Domain.Entities;
using WebApp.Domain.Enums;

namespace WebApp.Gateway.Cache;

/// <summary>
/// Cached app configuration. Excludes function code and static file content.
/// </summary>
public class CachedApp
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public AppStatus Status { get; set; }
    public Guid? LatestBuildId { get; set; }

    public CorsSettings CorsSettings { get; set; } = CorsSettings.Default;
    public RoutingConfig RoutingConfig { get; set; } = RoutingConfig.Default;
    public AppSettings Settings { get; set; } = AppSettings.Default;

    public List<CachedRoute> Routes { get; set; } = [];
    public List<CachedAuthenticationScheme> AuthenticationSchemes { get; set; } = [];
    public List<CachedVariable> Variables { get; set; } = [];
}

/// <summary>
/// Cached route. Excludes Response (function code or static content).
/// Only contains enabled and active routes.
/// </summary>
public class CachedRoute
{
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ResponseType ResponseType { get; set; }
    public int? ResponseStatusCode { get; set; }
    public IList<ResponseHeader> ResponseHeaders { get; set; } = [];
    public string? RequestQuerySchema { get; set; }
    public string? RequestHeaderSchema { get; set; }
    public string? RequestBodySchema { get; set; }
    public bool RequireAuthorization { get; set; }
    public FunctionRuntime? FunctionRuntime { get; set; }
}

/// <summary>
/// Cached authentication scheme. Only contains enabled schemes.
/// </summary>
public class CachedAuthenticationScheme
{
    public AuthenticationSchemeType Type { get; set; }
    public string? OpenIdConnectAuthority { get; set; }
    public string? OpenIdConnectAudience { get; set; }
}

public class CachedVariable
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
}
