using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Api.Models;
using WebApp.Infrastructure;

namespace WebApp.Api.Services;

public class AppBuildJobStatusConsumer(
    IConfiguration configuration,
    ILogger<AppBuildJobStatusConsumer> logger,
    IServiceProvider serviceProvider,
    IAppBuildPublisher publisher)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
        };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        
        const string exchange = "app.build.events";
        const string queue = "api." + exchange;
        
        channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueBind(queue, exchange, routingKey: "");
        
        var consumer = new EventingBasicConsumer(channel);
        
        //TODO: implement retry mechanism and dead-letter queue
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            await ProcessMessage(message);
            
            channel.BasicAck(ea.DeliveryTag, false);
        };
        
        channel.BasicConsume(queue, autoAck: false, consumer: consumer);
        
        return Task.CompletedTask;
    }

    private async Task ProcessMessage(string message)
    {
        var eventMessage = JsonSerializer.Deserialize<BuildStatusChangedEventMessage>(message)!;

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

    private async Task ProcessFailedMessage(BuildStatusChangedEventMessage message)
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
        await appDbContext.SaveChangesAsync();
    }

    private async Task ProcessDoneMessage(BuildStatusChangedEventMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

        // Update the job status
        logger.LogInformation("Updating job status. Id: {Id}, Status: {Status}", message.BuildId, message.Status);

        build.Status = "done";
        build.UpdatedAt = DateTime.UtcNow;
        
        await appDbContext.SaveChangesAsync();
    }

    private async Task ProcessStartedMessage(BuildStatusChangedEventMessage message)
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
        build.ContainerId = message.ContainerId;
        await appDbContext.SaveChangesAsync();
    }

    private async Task PostProcess(BuildStatusChangedEventMessage message)
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