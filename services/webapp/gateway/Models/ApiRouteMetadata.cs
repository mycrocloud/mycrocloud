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
    public string? Description { get; set; }
    public ResponseType ResponseType { get; set; }
    
    // Response metadata
    public ApiRouteResponseMetadata Response { get; set; } = new();

    // Request validation
    public string? RequestQuerySchema { get; set; }
    public string? RequestHeaderSchema { get; set; }
    public string? RequestBodySchema { get; set; }
    public bool RequireAuthorization { get; set; }
}

public class ApiRouteResponseMetadata
{
    public ApiStaticResponseMetadata? StaticResponse { get; set; }
    public ApiFunctionResponseMetadata? FunctionResponse { get; set; }
}

public class ApiStaticResponseMetadata
{
    public int? StatusCode { get; set; }
    public IList<ResponseHeader> Headers { get; set; } = [];
}

public class ApiFunctionResponseMetadata
{
    public FunctionRuntime? Runtime { get; set; }
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
