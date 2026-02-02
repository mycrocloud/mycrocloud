namespace WebApp.Domain.Services;

public interface ISubscriptionService
{
    Task<bool> CanCreateApp(string userId, int currentAppCount);
}

public class SubscriptionService : ISubscriptionService
{
    private const int DefaultAppLimit = 10;

    public Task<bool> CanCreateApp(string userId, int currentAppCount)
    {
        // TODO: check user plan and limit
        // For now, we use a hardcoded limit
        return Task.FromResult(currentAppCount < DefaultAppLimit);
    }
}
