namespace Api.Domain.Services;

public interface IApiDeploymentService
{
    /// <summary>
    /// Creates a new ApiDeployment by snapshotting the current routes of the app.
    /// Each function/static route's response will be saved as a blob.
    /// </summary>
    Task<Guid> CreateDeploymentSnapshotAsync(int appId, string? name = null, string? description = null);

    /// <summary>
    /// Creates initial snapshots for all legacy apps that don't have an active API deployment yet.
    /// </summary>
    Task<int> BootstrapLegacyAppsAsync();
}
