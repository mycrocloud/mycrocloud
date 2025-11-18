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
        var eventMessage = JsonSerializer.Deserialize<JobStatusChangedEventMessage>(message)!;

        switch (eventMessage.Status)
        {
            case JobStatus.Started:
                await ProcessStartedMessage(eventMessage);
                break;
            case JobStatus.Done:
                await ProcessDoneMessage(eventMessage);
                break;
            case JobStatus.Failed:
                await ProcessFailedMessage(eventMessage);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        await PostProcess(eventMessage);
    }

    private async Task ProcessFailedMessage(JobStatusChangedEventMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await appDbContext.AppBuildJobs.FindAsync(message.JobId);
        if (job == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.JobId);
            return;
        }

        job.Status = "failed";
        job.UpdatedAt = DateTime.UtcNow;
        await appDbContext.SaveChangesAsync();
    }

    private async Task ProcessDoneMessage(JobStatusChangedEventMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await appDbContext.AppBuildJobs.FindAsync(message.JobId);
        if (job == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.JobId);
            return;
        }

        var app = await appDbContext.Apps.FindAsync(job.AppId);
        if (app == null)
        {
            logger.LogWarning("App with id {AppId} not found", job.AppId);
            return;
        }

        // Update the job status
        logger.LogInformation("Updating job status. Id: {Id}, Status: {Status}", message.JobId, message.Status);

        job.Status = "done";
        job.UpdatedAt = DateTime.UtcNow;
        
        await appDbContext.SaveChangesAsync();
    }

    private async Task ProcessStartedMessage(JobStatusChangedEventMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await appDbContext.AppBuildJobs.FindAsync(message.JobId);
        if (job == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.JobId);
            return;
        }

        job.Status = "started";
        job.UpdatedAt = DateTime.UtcNow;
        job.ContainerId = message.ContainerId;
        await appDbContext.SaveChangesAsync();
    }

    private async Task PostProcess(JobStatusChangedEventMessage message)
    {
        using var scope = serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await appDbContext.AppBuildJobs.FindAsync(message.JobId);
        if (job == null)
        {
            logger.LogWarning("Job with id {Id} not found", message.JobId);
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