using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Domain.Messages;
using WebApp.Infrastructure;

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
        await appDbContext.SaveChangesAsync();
    }

    private async Task ProcessDoneMessage(BuildStatusChangedMessage message)
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

        app.LatestBuild = build;
        
        await appDbContext.SaveChangesAsync();
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
        build.ContainerId = message.ContainerId;
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