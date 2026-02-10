using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Api.Domain.Entities;
using Api.Domain.Messages;
using Api.Infrastructure;
using Api.Domain.Services;

namespace Api.Services;

public class AppBuildStatusConsumer(
    IConfiguration configuration,
    ILogger<AppBuildStatusConsumer> logger,
    IServiceProvider serviceProvider,
    IAppBuildPublisher publisher)
    : BackgroundService
{
    private IConnection? _connection;
    private IModel? _channel;
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        const string exchange = "app.build.events";
        const string queue = "api." + exchange;
        
        _channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        _channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        _channel.QueueBind(queue, exchange, routingKey: "");
        
        var consumer = new EventingBasicConsumer(_channel);
        
        //TODO: implement retry mechanism and dead-letter queue
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            await ProcessMessage(message);
            
            _channel.BasicAck(ea.DeliveryTag, false);
        };
        
        _channel.BasicConsume(queue, autoAck: false, consumer: consumer);
        
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _connection?.Close();
        
         await base.StopAsync(cancellationToken);
    }

    private async Task ProcessMessage(string message)
    {
        var eventMessage = JsonSerializer.Deserialize<BuildStatusChangedMessage>(message)!;

        switch (eventMessage.Status)
        {
            case BuildStatus.Started:
                await ProcessStartedMessage(eventMessage);
                break;
            case BuildStatus.Done:
                await ProcessDoneMessage(eventMessage);
                break;
            case BuildStatus.Failed:
                await ProcessFailedMessage(eventMessage);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await PostProcess(eventMessage);
    }

    private async Task ProcessFailedMessage(BuildStatusChangedMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var build = await appDbContext.AppBuildJobs.FindAsync(message.BuildId);
        if (build == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.BuildId);
            return;
        }

        build.Status = "failed";
        build.UpdatedAt = DateTime.UtcNow;
        build.FinishedAt = DateTime.UtcNow;

        // Update deployment status to Failed if exists
        var deployment = await appDbContext.SpaDeployments
            .FirstOrDefaultAsync(d => d.BuildId == message.BuildId);
        if (deployment != null)
        {
            deployment.Status = DeploymentStatus.Failed;
            deployment.UpdatedAt = DateTime.UtcNow;
        }

        await appDbContext.SaveChangesAsync();
    }

    private async Task ProcessDoneMessage(BuildStatusChangedMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var extractionService = scope.ServiceProvider.GetRequiredService<IArtifactExtractionService>();
        var cacheInvalidator = scope.ServiceProvider.GetRequiredService<IAppCacheInvalidator>();
        var specPublisher = scope.ServiceProvider.GetRequiredService<IAppSpecificationPublisher>();

        var build = await appDbContext.AppBuildJobs.FindAsync(message.BuildId);
        if (build == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.BuildId);
            return;
        }

        var app = await appDbContext.Apps.FindAsync(build.AppId);
        if (app == null)
        {
            logger.LogWarning("App with id {AppId} not found", build.AppId);
            return;
        }

        // Update build status
        build.Status = "done";
        build.UpdatedAt = DateTime.UtcNow;
        build.FinishedAt = DateTime.UtcNow;

        // Verify artifact exists
        if (!message.ArtifactId.HasValue)
        {
            logger.LogWarning("No artifact uploaded for build {BuildId}", message.BuildId);
            await appDbContext.SaveChangesAsync();
            return;
        }

        var artifact = await appDbContext.Artifacts.FindAsync(message.ArtifactId.Value);
        if (artifact == null)
        {
            logger.LogError("Artifact {ArtifactId} not found for build {BuildId}", message.ArtifactId, message.BuildId);
            build.Status = "failed";
            await appDbContext.SaveChangesAsync();
            return;
        }

        logger.LogInformation("Processing artifact {ArtifactId} ({SizeBytes} bytes) for build {BuildId}", 
            artifact.Id, artifact.BundleSize, build.Id);

        // Find existing deployment created when build started
        var deployment = await appDbContext.SpaDeployments
            .FirstOrDefaultAsync(d => d.BuildId == build.Id);

        if (deployment == null)
        {
            logger.LogWarning("No deployment found for build {BuildId}, creating new one", build.Id);
            deployment = new SpaDeployment
            {
                Id = Guid.NewGuid(),
                AppId = build.AppId,
                BuildId = build.Id,
                ArtifactId = artifact.Id,
                Status = DeploymentStatus.Pending
            };
            appDbContext.SpaDeployments.Add(deployment);
        }
        else
        {
            // Update existing deployment
            deployment.ArtifactId = artifact.Id;
            deployment.Status = DeploymentStatus.Pending;
            deployment.UpdatedAt = DateTime.UtcNow;
        }

        await appDbContext.SaveChangesAsync();

        // Extract zip to disk
        try
        {
            await extractionService.ExtractAsync(artifact.Id, deployment.Id, build.AppId);
            deployment.Status = DeploymentStatus.Ready;
            logger.LogInformation("Extracted artifact {ArtifactId} for deployment {DeploymentId}", 
                artifact.Id, deployment.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to extract artifact {ArtifactId}", artifact.Id);
            deployment.Status = DeploymentStatus.Failed;
            build.Status = "failed";
            await appDbContext.SaveChangesAsync();
            return;
        }

        // Create and activate release
        var release = new Release
        {
            Id = Guid.NewGuid(),
            AppId = build.AppId,
            SpaDeploymentId = deployment.Id
        };
        appDbContext.Releases.Add(release);
        app.ActiveReleaseId = release.Id;

        await appDbContext.SaveChangesAsync();

        logger.LogInformation("Created and activated release {ReleaseId} for app {AppId}", 
            release.Id, build.AppId);

        // Invalidate Gateway cache (Old mechanism)
        await cacheInvalidator.InvalidateByIdAsync(build.AppId);

        // Publish new AppSpecification (New mechanism)
        await specPublisher.PublishAsync(app.Slug);
    }

    private async Task ProcessStartedMessage(BuildStatusChangedMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var build = await appDbContext.AppBuildJobs.FindAsync(message.BuildId);
        if (build == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.BuildId);
            return;
        }

        build.Status = "started";
        build.UpdatedAt = DateTime.UtcNow;
        build.FinishedAt = null;
        await appDbContext.SaveChangesAsync();
    }

    private async Task PostProcess(BuildStatusChangedMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await appDbContext.AppBuildJobs.FindAsync(message.BuildId);
        if (job == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.BuildId);
            return;
        }
        
        var app = await appDbContext.Apps.FindAsync(job.AppId);
        if (app == null)
        {
            logger.LogWarning("App with id {AppId} not found", job.AppId);
            return;
        }
        
        publisher.Publish(app.Id, job.Status);
    }
}