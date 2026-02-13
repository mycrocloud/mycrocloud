using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Api.Domain.Models;

/// <summary>
/// AppSpecification: The immutable definition of an app at a specific release.
/// Gateway executes this specification to serve requests.
/// </summary>
public class AppSpecification
{
    public int Id { get; init; }
    public required string Slug { get; set; }
    public required string OwnerId { get; set; }
    public AppState State { get; set; }
    
    // SPA
    public Guid? SpaDeploymentId { get; init; }
    
    // API
    public Guid? ApiDeploymentId { get; init; }

    public CorsSettings ApiCorsSettings { get; set; }
    public RoutingConfig RoutingConfig { get; set; }
    public AppSettings Settings { get; set; }

    public List<CachedRoute> Routes { get; set; } = [];
    public List<CachedAuthenticationScheme> AuthenticationSchemes { get; set; } = [];
    
    /// <summary>
    /// Runtime variables for functions.
    /// </summary>
    public List<CachedVariable> Variables { get; set; } = [];
}

/// <summary>
/// Cached route. Excludes Response (function code or static content) and metadata (headers, schemas).
/// Only contains enabled and active routes.
/// </summary>
public class CachedRoute
{
    public int Id { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public ResponseType ResponseType { get; set; }
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
