namespace Api.Domain.Services;

public interface IOpenApiGenerator
{
    /// <summary>
    /// Generates an OpenAPI 3.0 specification from route metadata.
    /// </summary>
    string GenerateSpecification(string appName, string appSlug, IEnumerable<Models.ApiRouteMetadata> routes);
}
