namespace Api.Domain.Entities;

public enum RouteMatchType
{
    Prefix,
    Exact,
    Regex
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

public class RoutingConfigRoute
{
    public string? Name { get; set; }
    public int? Priority { get; set; }
    public RouteMatch Match { get; set; } = new();
    public RouteTarget Target { get; set; } = new();
}

public class RoutingConfig
{
    public string SchemaVersion { get; set; } = string.Empty;
    public List<RoutingConfigRoute> Routes { get; set; } = [];

    public static RoutingConfig Default => new()
    {
        SchemaVersion = "1.0",
        Routes =
        [
            new RoutingConfigRoute
            {
                Priority = 1,
                Match = new RouteMatch { Type = RouteMatchType.Prefix, Path = "/api" },
                Target = new RouteTarget { Type = RouteTargetType.Api, StripPrefix = false }
            },
            new RoutingConfigRoute
            {
                Priority = 2,
                Match = new RouteMatch { Type = RouteMatchType.Prefix, Path = "/" },
                Target = new RouteTarget { Type = RouteTargetType.Static, Fallback = "/index.html" }
            }
        ]
    };
}
