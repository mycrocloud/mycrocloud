using System.Text.Json;
using Api.Controllers;
using Api.Domain.Entities;
using Api.Domain.Messages;
using Api.Infrastructure;

using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BuildOrchestrationService(
    AppDbContext appDbContext,
    RabbitMqService rabbitMqService,
    IAppBuildPublisher publisher,
    GitHubAppService gitHubAppService,
    ILogger<BuildOrchestrationService> logger)
{
    /// <summary>
    /// Creates and queues a build job for the given app
    /// </summary>
    public async Task<AppBuild> CreateAndQueueBuildAsync(
        App app,
        string cloneUrl,
        string repoFullName,
        string artifactsUploadPath,
        Dictionary<string, string>? buildEnvVars = null,
        string? deploymentName = null)
    {
        // Fetch build environment variables if not provided
        buildEnvVars ??= await appDbContext.Variables
            .Where(v => v.AppId == app.Id && (v.Target == VariableTarget.Build || v.Target == VariableTarget.All))
            .ToDictionaryAsync(v => v.Name, v => v.Value ?? "");

        // Fetch latest commit info from GitHub
        var config = app.BuildConfigs ?? AppBuildConfigs.Default;
        var branch = string.IsNullOrEmpty(config.Branch) ? AppBuildConfigs.Default.Branch : config.Branch;
        
        GitHubCommitInfo? commitInfo = null;
        try
        {
            commitInfo = await gitHubAppService.GetLatestCommitByRepoId(
                app.Link!.InstallationId,
                app.Link.RepoId,
                branch
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch commit info for build");
        }

        var build = new AppBuild
        {
            Id = Guid.NewGuid(),
            App = app,
            Status = AppBuildState.queued,
            CreatedAt = DateTime.UtcNow,
            Metadata = new Dictionary<string, string>()
        };
        
        // Populate metadata if commit info is available
        if (commitInfo != null)
        {
            if (!string.IsNullOrEmpty(commitInfo.Sha))
                build.Metadata[BuildMetadataKeys.CommitSha] = commitInfo.Sha;
            if (!string.IsNullOrEmpty(commitInfo.Commit.Message))
                build.Metadata[BuildMetadataKeys.CommitMessage] = commitInfo.Commit.Message;
            if (!string.IsNullOrEmpty(commitInfo.Commit.Author.Name))
                build.Metadata[BuildMetadataKeys.Author] = commitInfo.Commit.Author.Name;
        }
        if (!string.IsNullOrEmpty(branch))
            build.Metadata[BuildMetadataKeys.Branch] = branch;

        appDbContext.AppBuildJobs.Add(build);

        // Create deployment immediately with Building status
        var deployment = new SpaDeployment
        {
            Id = Guid.NewGuid(),
            AppId = app.Id,
            BuildId = build.Id,
            ArtifactId = null,
            Name = deploymentName,
            Status = DeploymentStatus.Building
        };
        appDbContext.SpaDeployments.Add(deployment);

        var buildConfig = app.BuildConfigs ?? AppBuildConfigs.Default;

        // TODO: Get limits based on user's subscription plan
        // var planLimits = await GetUserPlanLimits(app.UserId);
        var planLimits = PlanLimits.Free;

        // Replace {buildId} placeholder in path with actual build ID
        var finalArtifactsUploadPath = artifactsUploadPath.Replace("{buildId}", build.Id.ToString());

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
            EnvVars = buildEnvVars,
            ArtifactsUploadPath = finalArtifactsUploadPath,
            LogsUploadPath = $"/apps/{app.Id}/builds/{build.Id}/logs",
            Limits = planLimits
        };

        rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));
            
        publisher.Publish(app.Id, build.Status);

        await appDbContext.SaveChangesAsync();

        logger.LogInformation("Created and queued build {BuildId} for app {AppId}", build.Id, app.Id);
        
        return build;
    }
}
