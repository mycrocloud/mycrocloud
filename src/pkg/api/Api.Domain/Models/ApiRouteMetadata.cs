using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Api.Domain.Models;

/// <summary>
/// Versioned metadata for an API route, stored as a blob in an ApiDeployment.
/// </summary>
public class ApiRouteMetadata
{
    // Display information
    public int Id { get; set; }
    public string Name { get; set; }
    public string Method { get; set; }
    public string Path { get; set; }
    public string Description { get; set; }
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
