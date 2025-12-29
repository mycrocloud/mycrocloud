using System.Text;
using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Api.Filters;
using WebApp.Api.Models.Builds;
using WebApp.Api.Services;
using WebApp.Domain.Entities;
using WebApp.Domain.Messages;
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
    RabbitMqService rabbitMqService,
    LinkGenerator linkGenerator,
    GitHubAppService gitHubAppService,
    ILogger<BuildsController> logger): BaseController
{
    public const string Controller = "Builds";

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
    
    [HttpPost("build")]
    public async Task<IActionResult> Build(int appId, BuildRequest request)
    {
        var app = await appDbContext.Apps
            .Include(a => a.Link)
            .SingleAsync(a => a.Id == appId);

        if (app.Link is null)
            return BadRequest();

        var installationAccessToken = await gitHubAppService.GetInstallationAccessToken(app.Link.InstallationId);

        var repos = await gitHubAppService.GetAccessibleRepos(app.Link.InstallationId, installationAccessToken);
        
        var repo = repos.SingleOrDefault(r => r.Id == app.Link.RepoId);
        if (repo is null)
            return BadRequest();
        
        var build = new AppBuild
        {
            Id = Guid.NewGuid(),
            App = app,
            Name = request.Name,
            Status = "Queued",
            CreatedAt = DateTime.UtcNow
        };

        appDbContext.AppBuildJobs.Add(build);

        var config = app.BuildConfigs;
        
        var message = new AppBuildMessage
        {
            BuildId = build.Id.ToString(),
            RepoFullName = repo.FullName,
            CloneUrl = $"https://x-access-token:{installationAccessToken}@github.com/{repo.FullName}",
            Branch = config.Branch,
            Directory = config.Directory,
            OutDir = config.OutDir,
            InstallCommand = config.InstallCommand,
            BuildCommand = config.BuildCommand,
            ArtifactsUploadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(BuildsController.PutObject), BuildsController.Controller, new { appId = app.Id, buildId = build.Id })!
        };

        rabbitMqService.PublishMessage(JsonSerializer.Serialize(message));
            
        publisher.Publish(app.Id, build.Status);

        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }
    
    [HttpGet("stream")]
    public async Task<IActionResult> StreamBuilds(int appId)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        Response.Headers.Append("X-Accel-Buffering", "no");   // disable Nginx buffering

        var cancellationToken = HttpContext.RequestAborted;

        await foreach (var msg in publisher.Subscribe(appId, cancellationToken))
        {
            await Response.WriteAsync($"data: {msg}\n\n", cancellationToken: cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }

        return new EmptyResult();
    }

    [HttpPost("config")]
    public async Task<IActionResult> Config(int appId, BuildConfigRequest config)
    {
        var app = await appDbContext.Apps
            .SingleAsync(a => a.Id == appId);

        app.BuildConfigs = new AppBuildConfigs()
        {
            Branch = config.Branch,
            Directory = config.Directory,
            BuildCommand = config.BuildCommand,
            OutDir = config.OutDir,
        };

        await appDbContext.SaveChangesAsync();
        
        return NoContent();
    }

    [HttpGet("config")]
    public async Task<IActionResult> Config(int appId)
    {
        var app = await appDbContext.Apps
            .SingleAsync(a => a.Id == appId);

        return Ok(app.BuildConfigs);
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
    public async Task<IActionResult> StreamBuildLogs(int appId, Guid buildId)
    {
        var build = await appDbContext.AppBuildJobs.SingleAsync(b => b.AppId == appId && b.Id == buildId);
        
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        
        Response.Headers.Append("X-Accel-Buffering", "no");   // disable Nginx buffering
        
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

        var rk = exchange + $".{build.Id.ToString()}";
        channel.QueueBind(
            queue: queueName,
            exchange: exchange,
            routingKey: rk
        );
        
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken: cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        };
        
        logger.LogInformation("Listening for build logs. exchange: {exchange}, queueName: {queueName}, routingKey: {routingKey}", exchange, queueName, rk);
        
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

    [HttpPut("{buildId:guid}/artifacts/{*key}")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [DisableAppOwnerActionFilter]
    [Authorize(Policy = "M2M", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> PutObject(int appId, Guid buildId, string key, [FromForm]IFormFile file)
    {
        var build = await appDbContext.AppBuildJobs
            .SingleAsync(b => b.AppId == appId && b.Id == buildId);

        await using var stream = file.OpenReadStream();
        await using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var content = memoryStream.ToArray();
    
        var dbFile = appDbContext.AppBuildArtifacts.SingleOrDefault(f => f.BuildId == build.Id && f.Path == key);

        if (dbFile is null)
        {
            dbFile = new AppBuildArtifact
            {
                Build = build,
                Path = key,
                Content = content
            };
            
            appDbContext.AppBuildArtifacts.Add(dbFile);
        }
        else
        {
            dbFile.Content = content;
        }

        await appDbContext.SaveChangesAsync();

        return Ok();
    }
}