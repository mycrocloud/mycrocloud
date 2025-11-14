using System.Text;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Api.Filters;
using WebApp.Api.Models.Builds;
using WebApp.Api.Services;
using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class BuildsController(
    AppDbContext appDbContext,
    IConfiguration configuration,
    [FromKeyedServices("AppBuildLogs_ES7")]
    ElasticClient elasticClient,
    [FromKeyedServices("AppBuildLogs_ES8")]
    ElasticsearchClient elasticsearchClient,
    IAppBuildPublisher publisher,
    ILogger<BuildsController> logger): BaseController
{
    [HttpGet]
    public async Task<IActionResult> List(int appId)
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
    
    [HttpGet("subscription")]
    public async Task<IActionResult> Subscribe(int appId)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var cancellationToken = HttpContext.RequestAborted;

        await foreach (var msg in publisher.Subscribe(appId, cancellationToken))
        {
            await Response.WriteAsync($"data: {msg}\n\n", cancellationToken: cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }

    [HttpPost("config")]
    [NonAction] //NOTE: not ready for production
    public async Task<IActionResult> Config(int appId, BuildConfigRequest buildConfigRequest)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Integration)
            .SingleAsync(a => a.Id == appId);

        if (app.Integration is null)
        {
            app.Integration = new AppIntegration
            {
                //Branch = buildConfigRequest.Branch,
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
    public async Task<IActionResult> Config(int appId)
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

    [HttpGet("{jobId:guid}/logs")]
    public async Task<IActionResult> Logs(int appId, Guid jobId)
    {
        var job = await appDbContext.AppBuildJobs
            .SingleAsync(j => j.AppId == appId && j.Id == jobId);

        IReadOnlyCollection<BuildLogDoc> docs;
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

            if (!response.IsValidResponse)
            {
                logger.LogError("Elasticsearch response is not valid: {Error}", response.DebugInformation);
            }
            
            docs = response.Documents;
        }
        else
        {
            var response = await elasticClient.SearchAsync<BuildLogDoc>(s =>
                s.Query(q => q
                    .Match(m => m
                        .Field("job_id")
                        .Query(job.Id.ToString())
                    )
                )
            );
            
            if (!response.IsValid)
            {
                logger.LogError("Elasticsearch response is not valid: {Error}", response.DebugInformation);
            }

            docs = response.Documents;
        }
        
        return Ok(docs.Select(d => new
        {
            d.Timestamp,
            d.Message,
            d.Level
        }));
    }
    
    [HttpGet("{buildId:guid}/logs/stream")]
    public async Task<IActionResult> Stream(int appId, Guid buildId)
    {
        var build = await appDbContext.AppBuildJobs.SingleAsync(b => b.AppId == appId && b.Id == buildId);
        
        // use server sent events to stream logs
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        var cancellationToken = HttpContext.RequestAborted;
       
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
        };
        
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        const string exchange = "app.build.logs";
        
        channel.ExchangeDeclare(exchange: exchange, type: "topic", durable: false); //TODO: confirm durable setting
        
        var requestId = HttpContext.TraceIdentifier;
        var queueName = exchange + $".{build.Id}_{requestId}"; // unique queue name per request
        
        channel.QueueDeclare(
            queue: queueName,
            durable: false,
            exclusive: true,
            autoDelete: true
        );
        
        channel.QueueBind(
            queue: queueName,
            exchange: exchange,
            routingKey: exchange + $".{build.Id}"
        );
        
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken: cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        };
        
        channel.BasicConsume(
            queue: queueName,
            autoAck: true,
            consumer: consumer
        );
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }
        }
        finally
        {
            channel.BasicCancel(consumer.ConsumerTags[0]);
            channel.Close();
            connection.Close();
        }

        return new EmptyResult();
    }
}