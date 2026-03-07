namespace MycroCloud.WebApp.Gateway.Models;

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

    public required CorsSettings ApiCorsSettings { get; set; }
    public required RoutingConfig RoutingConfig { get; set; }
    public required AppSettings Settings { get; set; }

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
public class ApiRouteSummary
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

public enum ResponseType
{
    Static = 1,
    StaticFile = 2,
    Function = 3
}

public enum AuthenticationSchemeType
{
    OpenIdConnect = 1,
    Basic = 3
}

public class AppSettings
{
    public bool CheckFunctionExecutionLimitMemory { get; set; }
    public long? FunctionExecutionLimitMemoryBytes { get; set; }
    public bool CheckFunctionExecutionTimeout { get; set; }
    public int? FunctionExecutionTimeoutSeconds { get; set; }
    public bool FunctionUseNoSqlConnection { get; set; }
    
    public static AppSettings Default => new()
    {
        CheckFunctionExecutionLimitMemory = true,
        FunctionExecutionLimitMemoryBytes = 2 * 1024 * 1024,
        CheckFunctionExecutionTimeout = true,
        FunctionExecutionTimeoutSeconds = 10,
        FunctionUseNoSqlConnection = false
    };
}
public enum AppState
{
    Active = 1,
    Disabled,
    Deleted
}

public class CorsSettings
{
    public List<string> AllowedHeaders { get; set; } = [];
    public List<string> AllowedMethods { get; set; } = [];
    public List<string> AllowedOrigins { get; set; } = [];
    public List<string> ExposeHeaders { get; set; } = [];
    public int? MaxAgeSeconds { get; set; }
}

public class RoutingConfig
{
    public string SchemaVersion { get; set; } = string.Empty;
    public List<RoutingConfigRoute> Routes { get; set; } = [];
}

public class RoutingConfigRoute
{
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public RouteMatch Match { get; set; } = new();
    public RouteTarget Target { get; set; } = new();
}

public enum RouteTargetType
{
    Api,
    Static
}

public class RouteMatch
{
    public RouteMatchType Type { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class RouteTarget
{
    public RouteTargetType Type { get; set; }
    public bool? StripPrefix { get; set; }
    public string? Rewrite { get; set; }
    public string? Fallback { get; set; }
}

public enum RouteMatchType
{
    Prefix,
    Exact,
    Regex
}
