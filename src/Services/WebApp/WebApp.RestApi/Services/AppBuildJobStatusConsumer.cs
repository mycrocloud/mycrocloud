using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.RestApi.Services;

public class AppBuildJobStatusConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppBuildJobStatusConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IConnection _connection;
    private IModel _channel;

    public AppBuildJobStatusConsumer(
        IConfiguration configuration,
        ILogger<AppBuildJobStatusConsumer> logger,
        IServiceProvider serviceProvider
    )
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
        InitRabbitMq();
    }

    private void InitRabbitMq()
    {
        // Create a connection factory
        var factory = new ConnectionFactory
        {
            Uri = new Uri(_configuration.GetConnectionString("RabbitMq")!),
        };

        // Create a connection and a channel
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare a queue (ensure the queue exists)
        _channel.QueueDeclare(queue: "job_status", // Name of the queue
            durable: true, // Durable queue (persists)
            exclusive: false, // Not exclusive to one consumer
            autoDelete: false, // Do not auto-delete the queue
            arguments: null); // No additional arguments
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        InitRabbitMq();
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            await ProcessMessage(JsonSerializer.Deserialize<AppBuildJobStatusMessage>(message)!);
        };

        _channel.BasicConsume(queue: "job_status", // Name of the queue
            autoAck: true, // Auto-acknowledge the message
            consumer: consumer); // The consumer to use

        return Task.CompletedTask;
    }

    private async Task ProcessMessage(AppBuildJobStatusMessage message)
    {
        _logger.LogInformation("Received message. Id: {Id}, Status: {Status}", message.Id, message.Status);

        using var scope = _serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var job = await appDbContext.AppBuildJobs.FindAsync(message.Id);
        if (job == null)
        {
            _logger.LogWarning("Job with id {Id} not found", message.Id);
            return;
        }

        var objects = appDbContext.Objects
            .Where(obj => obj.Key.StartsWith(job.Id))
            .AsNoTracking()
            .ToList();

        var app = await appDbContext.Apps.FindAsync(job.AppId);
        if (app == null)
        {
            _logger.LogWarning("App with id {AppId} not found", job.AppId);
            return;
        }

        foreach (var obj in objects)
        {
            obj.AppId = app.Id;
            obj.Key = obj.Key[job.Id.Length..];
            obj.Type = ObjectType.BuildArtifact;
        }

        var currentObjects = await appDbContext.Objects
            .Where(obj => obj.AppId == app.Id && obj.Type == ObjectType.BuildArtifact)
            .ToListAsync();
        
        appDbContext.Objects.RemoveRange(currentObjects);

        await appDbContext.Objects.AddRangeAsync(objects);

        job.Status = message.Status;
        job.UpdatedAt = DateTime.UtcNow;
        await appDbContext.SaveChangesAsync();
    }
}

public class AppBuildJobStatusMessage
{
    public string Id { get; set; }

    public string Status { get; set; }
}