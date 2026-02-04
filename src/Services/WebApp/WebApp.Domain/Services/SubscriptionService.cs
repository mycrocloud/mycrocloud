namespace WebApp.Domain.Services;

public interface ISubscriptionService
{
    Task<bool> CanCreateApp(string userId, int currentAppCount);
    Task<bool> CanCreateRoutes(string userId, int currentRouteCount, int routesToAdd = 1);
}

public class SubscriptionService : ISubscriptionService
{
    private const int DefaultAppLimit = 10;
    private const int DefaultRouteLimit = 1000;

    public Task<bool> CanCreateApp(string userId, int currentAppCount)
    {
        // TODO: check user plan and limit
        // For now, we use a hardcoded limit
        return Task.FromResult(currentAppCount < DefaultAppLimit);
    }

    public Task<bool> CanCreateRoutes(string userId, int currentRouteCount, int routesToAdd = 1)
    {
        // TODO: check user plan and limit
        // For now, we use a hardcoded limit
        return Task.FromResult(currentRouteCount + routesToAdd <= DefaultRouteLimit);
    }
}
