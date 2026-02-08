using WebApp.Domain.Entities;
using WebApp.Domain.Enums;

namespace WebApp.Domain.Models;

/// <summary>
/// Versioned metadata for an API route, stored as a blob in an ApiDeployment.
/// </summary>
public class ApiRouteMetadata
{
    public int? ResponseStatusCode { get; set; }
    public IList<ResponseHeader> ResponseHeaders { get; set; } = [];
    public string? RequestQuerySchema { get; set; }
    public string? RequestHeaderSchema { get; set; }
    public string? RequestBodySchema { get; set; }
    public bool RequireAuthorization { get; set; }
    public FunctionRuntime? FunctionRuntime { get; set; }
}
