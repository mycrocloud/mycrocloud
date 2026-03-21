using System.Text.Json;
using Api.Controllers;
using Api.Domain.Entities;
using Api.Domain.Messages;
using Api.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BuildOrchestrationService(
    AppDbContext appDbContext,
    BuildQueuePublisher buildQueuePublisher,
    IAppBuildPublisher publisher,
    GitHubAppService gitHubAppService,
    IConfiguration configuration,
    ILogger<BuildOrchestrationService> logger)
{
    /// <summary>
    /// Creates build + deployment records in DB and publishes SSE. Fast — no GitHub calls.
    /// </summary>
    public async Task<AppBuild> CreateBuildAsync(App app, string? deploymentName = null)
    {
        var build = new AppBuild
        {
            Id = Guid.NewGuid(),
            App = app,
            Status = AppBuildState.queued,
            CreatedAt = DateTime.UtcNow,
            Metadata = []
        };

        appDbContext.AppBuildJobs.Add(build);
        appDbContext.SpaDeployments.Add(new SpaDeployment
        {
            Id = Guid.NewGuid(),
            AppId = app.Id,
            BuildId = build.Id,
            ArtifactId = null,
            Name = deploymentName,
            Status = DeploymentStatus.Building
        });

        await appDbContext.SaveChangesAsync();

        publisher.Publish(app.Id, build.Status);

        logger.LogInformation("Created build {BuildId} for app {AppId}", build.Id, app.Id);

        return build;
    }

    /// <summary>
    /// Fetches commit info, env vars, then publishes the build message to the queue.
    /// Designed to run after <see cref="CreateBuildAsync"/>, optionally in a background scope.
    /// </summary>
    public async Task QueueBuildAsync(Guid buildId, int appId, string cloneUrl, string repoFullName)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Link)
            .SingleAsync(a => a.Id == appId);

        var build = await appDbContext.AppBuildJobs.FindAsync(buildId)
            ?? throw new InvalidOperationException($"Build {buildId} not found");

        var buildEnvVars = await appDbContext.Variables
            .Where(v => v.AppId == appId && (v.Target == VariableTarget.Build || v.Target == VariableTarget.All))
            .ToDictionaryAsync(v => v.Name, v => v.Value ?? "");

        var config = app.BuildConfigs ?? AppBuildConfigs.Default;
        var branch = string.IsNullOrEmpty(config.Branch) ? AppBuildConfigs.Default.Branch : config.Branch;

        try
        {
            var commitInfo = await gitHubAppService.GetLatestCommitByRepoId(
                app.Link!.InstallationId,
                app.Link.RepoId,
                branch
            );
            if (commitInfo != null)
            {
                if (!string.IsNullOrEmpty(commitInfo.Sha))
                    build.Metadata[BuildMetadataKeys.CommitSha] = commitInfo.Sha;
                if (!string.IsNullOrEmpty(commitInfo.Commit.Message))
                    build.Metadata[BuildMetadataKeys.CommitMessage] = commitInfo.Commit.Message;
                if (!string.IsNullOrEmpty(commitInfo.Commit.Author.Name))
                    build.Metadata[BuildMetadataKeys.Author] = commitInfo.Commit.Author.Name;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch commit info for build {BuildId}", buildId);
        }

        if (!string.IsNullOrEmpty(branch))
            build.Metadata[BuildMetadataKeys.Branch] = branch;

        await appDbContext.SaveChangesAsync();

        var buildConfig = app.BuildConfigs ?? AppBuildConfigs.Default;
        var nodeVersion = string.IsNullOrEmpty(buildConfig.NodeVersion) ? "20" : buildConfig.NodeVersion;
        var builderImage = configuration["Build:BuilderImageTemplate"]!.Replace("{version}", nodeVersion);

        var message = new AppBuildMessage
        {
            BuildId = build.Id.ToString(),
            RepoFullName = repoFullName,
            CloneUrl = cloneUrl,
            Branch = buildConfig.Branch,
            Directory = buildConfig.Directory,
            OutDir = buildConfig.OutDir,
            InstallCommand = buildConfig.InstallCommand,
            BuildCommand = buildConfig.BuildCommand,
            NodeVersion = buildConfig.NodeVersion,
            BuilderImage = builderImage,
            EnvVars = buildEnvVars,
            ArtifactsUploadPath = $"/apps/{appId}/spa/builds/{buildId}/artifacts",
            LogsUploadPath = $"/apps/{appId}/spa/builds/{buildId}/logs",
            Limits = PlanLimits.Free
        };

        await buildQueuePublisher.PublishAsync(build.Id, JsonSerializer.Serialize(message));

        logger.LogInformation("Queued build {BuildId} for app {AppId}", buildId, appId);
    }

    public async Task MarkBuildFailedAsync(Guid buildId)
    {
        var build = await appDbContext.AppBuildJobs.FindAsync(buildId);
        if (build is null) return;

        build.Status = AppBuildState.failed;
        build.FinishedAt = DateTime.UtcNow;

        var deployment = await appDbContext.SpaDeployments.FirstOrDefaultAsync(d => d.BuildId == buildId);
        if (deployment is not null)
            deployment.Status = DeploymentStatus.Failed;

        await appDbContext.SaveChangesAsync();
        publisher.Publish(build.AppId, build.Status);
    }

}
