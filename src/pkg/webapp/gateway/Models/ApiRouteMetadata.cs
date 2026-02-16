namespace MycroCloud.WebApp.Gateway.Models;

/// <summary>
/// Versioned metadata for an API route, stored as a blob in an ApiDeployment.
/// </summary>
public class ApiRouteMetadata
{
    // Display information
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Method { get; set; }
    public required string Path { get; set; }
    public ResponseType ResponseType { get; set; }
    
    // Request validation and runtime configuration
    public int? ResponseStatusCode { get; set; }
    public IList<ResponseHeader> ResponseHeaders { get; set; } = [];
    public string? RequestQuerySchema { get; set; }
    public string? RequestHeaderSchema { get; set; }
    public string? RequestBodySchema { get; set; }
    public bool RequireAuthorization { get; set; }
    public FunctionRuntime? FunctionRuntime { get; set; }
}

public class ResponseHeader
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

public enum FunctionRuntime
{
    JintInDocker = 2,
}
