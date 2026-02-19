using Api.Domain.Models;

namespace Api.Domain.Services;

/// <summary>
/// Interface for publishing AppSpecification to the Data Plane (Gateway).
/// Generally implemented in the API service/Infrastruture layer.
/// </summary>
public interface IAppSpecificationPublisher
{
    /// <summary>
    /// Aggregates app data, creates an AppSpecification, and pushes it to Redis.
    /// </summary>
    Task PublishAsync(string slug);

    /// <summary>
    /// Removes the AppSpecification from Redis.
    /// </summary>
    Task InvalidateAsync(string slug);

    /// <summary>
    /// Removes the AppSpecification from Redis by App ID.
    /// </summary>
    Task InvalidateByIdAsync(int appId);

    /// <summary>
    /// Removes a custom domain index entry from Redis.
    /// </summary>
    Task InvalidateCustomDomainAsync(string domain);
}
