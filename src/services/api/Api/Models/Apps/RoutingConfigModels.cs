using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.Models.Apps;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RouteMatchType
{
    Prefix,
    Exact,
    Regex
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RouteTargetType
{
    Api,
    Static
}

public class RouteMatch
{
    [Required]
    public RouteMatchType Type { get; set; }

    [Required]
    public string Path { get; set; } = string.Empty;
}

public class RouteTarget
{
    [Required]
    public RouteTargetType Type { get; set; }

    public bool? StripPrefix { get; set; }

    public string? Rewrite { get; set; }

    public string? Fallback { get; set; }
}

public class Route
{
    public string? Name { get; set; }

    public int? Priority { get; set; }

    [Required]
    public RouteMatch Match { get; set; } = new();

    [Required]
    public RouteTarget Target { get; set; } = new();
}

public class RoutingConfig
{
    [Required]
    public string SchemaVersion { get; set; } = string.Empty;

    [Required]
    public List<Route> Routes { get; set; } = new();
}

public class UpdateRoutingConfigRequest
{
    [Required]
    public string SchemaVersion { get; set; } = string.Empty;

    [Required]
    public List<Route> Routes { get; set; } = new();
}