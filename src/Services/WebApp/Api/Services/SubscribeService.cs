using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Api.Domain.Messages;
using Api.Infrastructure;

namespace Api.Services;

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
        
        var eventMessage = JsonSerializer.Deserialize<BuildStatusChangedMessage>(message)!;
        
        using var scope = serviceScopeFactory.CreateScope();
        
        var slackAppService = scope.ServiceProvider.GetRequiredService<SlackAppService>();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var build = await appDbContext.AppBuildJobs
            .Include(b => b.App)
            .SingleAsync(b => b.Id == eventMessage.BuildId);
        
        var subscriptions = await appDbContext.SlackAppSubscriptions.Where(s => s.AppId == build.AppId)
            .ToListAsync();
        
        var emoji = eventMessage.Status switch
        {
            BuildStatus.Started => "🟡",
            BuildStatus.Done => "✅",
            BuildStatus.Failed => "❌",
            _ => "ℹ️"
        };
        
        var text = eventMessage.Status switch
        {
            BuildStatus.Started => $"{emoji} *Build started* for *{build.App.Slug}* (Build #{build.Id})",
            BuildStatus.Done => $"{emoji} *Build completed successfully!* 🎉\nApp: *{build.App.Slug}*  \nBuild Id: `{build.Id}`",
            BuildStatus.Failed => $"{emoji} *Build failed!* 💥\nApp: *{build.App.Slug}*  \nBuild Id: `{build.Id}`",
            _ => $"{emoji} Build status changed for *{build.App.Slug}*"
        };
        
        var webOrigin = configuration.GetValue<string>("WebOrigin")!.TrimEnd('/');
        var detailsUrl = $"{webOrigin}/apps/{build.AppId}/integrations/builds/{build.Id}";
        text += $"\n<{detailsUrl}|View build details>";
        
        foreach (var subscription in subscriptions)
        {
            await slackAppService.SendSlackMessage(subscription.TeamId, subscription.ChannelId, text);
        }
    }
}