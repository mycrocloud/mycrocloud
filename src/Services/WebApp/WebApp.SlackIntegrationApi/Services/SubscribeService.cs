using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Infrastructure;

namespace WebApp.SlackIntegrationApi.Services;

public class SubscribeService(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, ILogger<SubscribeService> logger) : BackgroundService
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
        const string queue = "slack_integration_api." + exchange;
        
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
        logger.LogInformation(message);
        
        var eventMessage = JsonSerializer.Deserialize<JobStatusChangedEventMessage>(message)!;
        
        using var scope = serviceScopeFactory.CreateScope();
        
        var slackAppService = scope.ServiceProvider.GetRequiredService<SlackAppService>();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        logger.LogInformation("Processing job status change for Job ID: {JobId}, Status: {Status}", eventMessage.JobId, eventMessage.Status);
        var buildJob = await appDbContext.AppBuildJobs
            .Include(b => b.App)
            .SingleAsync(b => b.Id == eventMessage.JobId);
        
        var subscriptions = await appDbContext.SlackAppSubscriptions.Where(s => s.AppId == buildJob.AppId)
            .ToListAsync();

        var emoji = eventMessage.Status switch
        {
            JobStatus.Started => "🟡",
            JobStatus.Done => "✅",
            JobStatus.Failed => "❌",
            _ => "ℹ️"
        };

        var text = eventMessage.Status switch
        {
            JobStatus.Started => $"{emoji} *Build started* for *{buildJob.App.Name}* (Job #{buildJob.Id})",
            JobStatus.Done => $"{emoji} *Build completed successfully!* 🎉\nApp: *{buildJob.App.Name}*  \nJob ID: `{buildJob.Id}`",
            JobStatus.Failed => $"{emoji} *Build failed!* 💥\nApp: *{buildJob.App.Name}*  \nJob ID: `{buildJob.Id}`",
            _ => $"{emoji} Build status changed for *{buildJob.App.Name}*"
        };

        var webOrigin = configuration.GetValue<string>("WebOrigin")!.TrimEnd('/');
        var detailsUrl = $"{webOrigin}/apps/{buildJob.AppId}/integrations/builds/{buildJob.Id}";
        text += $"\n<{detailsUrl}|View build details>";

        foreach (var subscription in subscriptions)
        {
            await slackAppService.SendSlackMessage(subscription.TeamId, subscription.ChannelId, text);
        }

        logger.LogInformation("Processed job status change for Job ID: {JobId}", eventMessage.JobId);
    }
}

public class JobStatusChangedEventMessage
{
    [JsonPropertyName("job_id")]
    public required Guid JobId { get; set; }
    
    [JsonPropertyName("status")]
    public JobStatus Status { get; set; }
}

public enum JobStatus
{
    Started,
    Done,
    Failed,
}