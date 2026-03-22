using Api.Domain.Entities;
using Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class OrphanBuildCleanupService(
    IServiceScopeFactory scopeFactory,
    IAppBuildPublisher publisher,
    ILogger<OrphanBuildCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan QueuedTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StartedTimeout = TimeSpan.FromMinutes(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);
                await ExpireOrphanBuildsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during orphan build cleanup");
            }
        }
    }

    private async Task ExpireOrphanBuildsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        var orphans = await dbContext.AppBuildJobs
            .Where(b =>
                (b.Status == AppBuildState.queued && b.CreatedAt < now - QueuedTimeout) ||
                (b.Status == "started" && b.CreatedAt < now - StartedTimeout))
            .ToListAsync(cancellationToken);

        if (orphans.Count == 0)
            return;

        logger.LogWarning("Expiring {Count} orphan build(s)", orphans.Count);

        foreach (var build in orphans)
        {
            var age = now - build.CreatedAt;
            logger.LogWarning("Expiring orphan build {BuildId} (status={Status}, age={Age:mm\\:ss})",
                build.Id, build.Status, age);

            build.Status = AppBuildState.failed;
            build.FinishedAt = now;
            build.UpdatedAt = now;

            var deployment = await dbContext.SpaDeployments
                .FirstOrDefaultAsync(d => d.BuildId == build.Id, cancellationToken);

            if (deployment is { Status: DeploymentStatus.Building })
            {
                deployment.Status = DeploymentStatus.Failed;
                deployment.UpdatedAt = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var build in orphans)
            publisher.Publish(build.AppId, AppBuildState.failed);
    }
}
