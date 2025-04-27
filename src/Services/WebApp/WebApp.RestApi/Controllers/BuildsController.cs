using System.Collections;
using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;
using WebApp.RestApi.Filters;
using WebApp.RestApi.Models.Builds;

namespace WebApp.RestApi.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class BuildsController(
    AppDbContext appDbContext,
    IConfiguration configuration,
    [FromKeyedServices("AppBuildLogs_ES7")]
    ElasticClient elasticClient,
    [FromKeyedServices("AppBuildLogs_ES8")]
    ElasticsearchClient elasticsearchClient
    ): BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetBuilds(int appId)
    {
        var jobs = await appDbContext.AppBuildJobs
            .Where(j => j.AppId == appId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();
        return Ok(jobs.Select(job => new
        {
            job.Id,
            job.Name,
            job.Status,
            job.CreatedAt,
            job.UpdatedAt,
        }));
    }

    [HttpPost("config")]
    public async Task<IActionResult> PostConfigBuild(int appId, BuildConfigRequest buildConfigRequest)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Integration)
            .SingleAsync(a => a.Id == appId);

        if (app.Integration is null)
        {
            app.Integration = new AppIntegration
            {
                Branch = buildConfigRequest.Branch,
                Directory = buildConfigRequest.Directory,
                BuildCommand = buildConfigRequest.BuildCommand,
                OutDir = buildConfigRequest.OutDir,
                CreatedAt = DateTime.UtcNow
            };
        }
        else
        {
            app.Integration.Branch = buildConfigRequest.Branch;
            app.Integration.Directory = buildConfigRequest.Directory;
            app.Integration.BuildCommand = buildConfigRequest.BuildCommand;
            app.Integration.OutDir = buildConfigRequest.OutDir;
            app.Integration.UpdatedAt = DateTime.UtcNow;
        }

        await appDbContext.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("config")]
    public async Task<IActionResult> GetConfigBuild(int appId)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Integration)
            .SingleAsync(a => a.Id == appId);

        if (app.Integration is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            app.Integration.Branch,
            app.Integration.Directory,
            app.Integration.BuildCommand,
            app.Integration.OutDir,
            app.Integration.CreatedAt,
            app.Integration.UpdatedAt
        });
    }

    [HttpGet("{jobId}/logs")]
    public async Task<IActionResult> GetBuildLogs(int appId, string jobId)
    {
        var job = await appDbContext.AppBuildJobs
            .SingleAsync(j => j.AppId == appId && j.Id == jobId);

        if (configuration["Elasticsearch:Version"] == "v8")
        {
            var response = await elasticsearchClient.SearchAsync<BuildLogDoc>(s =>
                s.Query(q => q
                    .Match(m => m
                        .Field("job_id")
                        .Query(job.Id)
                    )
                )
            );
            
            return Ok(response.Documents.Select(d => new
            {
                d.Timestamp,
                d.Message,
                d.Level
            }));
        }
        else
        {
            var response = await elasticClient.SearchAsync<BuildLogDoc>(s =>
                s.Query(q => q
                    .Match(m => m
                        .Field("job_id")
                        .Query(job.Id)
                    )
                )
            );

            return Ok(response.Documents.Select(d => new
            {
                d.Timestamp,
                d.Message,
                d.Level
            }));
        }
    }
    
    [HttpGet("{jobId}/logs/stream")]
    public async Task<IActionResult> Stream(int appId, string jobId)
    {
        // use server sent events to stream logs
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        var cancellationToken = HttpContext.RequestAborted;
       
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
        };
        
        // Create a connection and a channel
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        var queueName = $"logs-{jobId}";
        
        channel.QueueDeclare(queue: queueName, // Name of the queue
            durable: true, // Durable queue (persists)
            exclusive: false, // Not exclusive to one consumer
            autoDelete: false, // Do not auto-delete the queue
            arguments: null); // No additional arguments
        
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            var log = JsonSerializer.Deserialize<BuildLogDoc>(message);
            
            // send the log to the client
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(log)}\n\n", cancellationToken: cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        };
        
        channel.BasicConsume(queue: queueName, // Name of the queue
            autoAck: true, // Auto-acknowledge the message
            consumer: consumer); // The consumer to us
        
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken); // just wait, messages come via event handler
        }
        // clean up
        channel.BasicCancel(consumer.ConsumerTags[0]);
        
        // return a 200 OK response
        return Ok(); 
    }

    private async Task<IEnumerable<BuildLogDoc>> CreateMockLogsAsync(string jobId)
    {
        var logs = new List<BuildLogDoc>();
        for (int i = 0; i < 10; i++)
        {
            logs.Add(new BuildLogDoc
            {
                Source = "build",
                Timestamp = DateTime.UtcNow,
                Message = $"Log message {i}",
                Level = "info"
            });
        }
        
        // simulate a delay
        await Task.Delay(1000);
        return logs;
    }
}