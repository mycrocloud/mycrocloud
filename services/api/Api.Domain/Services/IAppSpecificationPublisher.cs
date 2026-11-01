namespace Api.Domain.Services;

/// <summary>Publishes durable runtime specifications and optionally pre-warms route caches.</summary>
public interface IAppSpecificationPublisher
{
    Task PublishAsync(string slug);
    Task PublishByIdAsync(int appId);
}
