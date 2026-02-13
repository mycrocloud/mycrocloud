using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Middlewares.Api;

/// <summary>
/// Interface for handling different types of API responses.
/// Handlers are responsible for generating the final HTTP response based on the route configuration.
/// </summary>
public interface IResponseHandler
{
    /// <summary>
    /// The response type that this handler supports.
    /// </summary>
    ResponseType SupportedType { get; }

    /// <summary>
    /// Handles the HTTP request and generates the response.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    Task HandleAsync(HttpContext context);
}
