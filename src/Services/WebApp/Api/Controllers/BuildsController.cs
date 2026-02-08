using System.Text;
using System.Text.Json;
using Api.Filters;
using Api.Models.Builds;
using Api.Services;
using WebApp.Domain.Services;
using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApp.Domain.Entities;
using WebApp.Domain.Messages;
using WebApp.Infrastructure;

namespace Api.Controllers;

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
    IStorageProvider storageProvider,
    ILogger<BuildsController> logger): BaseController
{
    public const string Controller = "Builds";

    [HttpGet]
    public async Task<IActionResult> List(int appId)
    {
        var builds = await appDbContext.AppBuildJobs
            .Where(b => b.AppId == appId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        
        return Ok(builds.Select(b => new
        {
            b.Id,
            b.Status,
            b.CreatedAt,
            b.UpdatedAt,
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

        // Fetch build environment variables
        var buildEnvVars = await appDbContext.Variables
            .Where(v => v.AppId == appId && (v.Target == VariableTarget.Build || v.Target == VariableTarget.All))
            .ToDictionaryAsync(v => v.Name, v => v.Value ?? "");

        var build = new AppBuild
        {
            Id = Guid.NewGuid(),
            App = app,
            Status = AppBuildState.queued,
            CreatedAt = DateTime.UtcNow
        };

        appDbContext.AppBuildJobs.Add(build);

        var config = app.BuildConfigs;

        // TODO: Get limits based on user's subscription plan
        // var planLimits = await GetUserPlanLimits(app.UserId);
        var planLimits = PlanLimits.Free;

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
            NodeVersion = config.NodeVersion,
            EnvVars = buildEnvVars,
            ArtifactsUploadUrl = linkGenerator.GetUriByAction(HttpContext, nameof(BuildsController.UploadArtifacts), BuildsController.Controller, new { appId = app.Id, buildId = build.Id })!,
            Limits = planLimits
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
            InstallCommand = config.InstallCommand,
            BuildCommand = config.BuildCommand,
            OutDir = config.OutDir,
            NodeVersion = config.NodeVersion,
            Framework = config.Framework,
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

    // Artifact Upload — Phase 1 design
    // -------------------------------
    // Worker computes SHA256 hash before upload.
    // API verifies hash and stores artifact blob in DB.
    // Artifact is deduplicated by ContentHash (UNIQUE index).
    // BuildArtifact junction links build → artifact.
    // This endpoint must be idempotent and retry-safe.
    //
    // Future phase:
    // - move blob to object storage
    // - stream hash instead of memory copy
    // - chunked upload
    [HttpPut("{buildId:guid}/artifacts")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [DisableAppOwnerActionFilter]
    [Authorize(Policy = "M2M", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UploadArtifacts(int appId, Guid buildId, [FromForm] IFormFile file, [FromForm] string contentHash)
    {
        var build = await appDbContext.AppBuildJobs
            .SingleAsync(b => b.AppId == appId && b.Id == buildId);

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only zip files are supported");
        }

        if (string.IsNullOrEmpty(contentHash))
        {
            return BadRequest("contentHash is required");
        }

        // 1. Save ZIP to storage
        var artifactId = Guid.NewGuid();
        var storageKey = $"artifacts/{appId}/{artifactId}.zip";
        
        await using (var stream = file.OpenReadStream())
        {
            await storageProvider.SaveAsync(storageKey, stream);
        }

        // 2. Verify hash (optional but good for integrity)
        // In a production environment, we might compute hash while streaming to storage
        // For now, we'll reopen to verify if needed, or trust the worker if we move hash computation there.
        // Let's reopening to compute hash to be sure.
        using (var verifyStream = await storageProvider.OpenReadAsync(storageKey))
        {
            var computedHash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(verifyStream));
            if (!computedHash.Equals(contentHash, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Hash mismatch for build {BuildId}. Expected: {Expected}, Got: {Got}", 
                    buildId, contentHash, computedHash);
                await storageProvider.DeleteAsync(storageKey);
                return BadRequest("Content hash mismatch");
            }
        }

        // 3. Create Artifact metadata
        var artifact = new Artifact
        {
            Id = artifactId,
            AppId = appId,
            BundleHash = contentHash.ToUpperInvariant(),
            BundleSize = file.Length,
            StorageType = ArtifactStorageType.Disk,
            StorageKey = storageKey,
            Compression = "zip"
        };
        appDbContext.Artifacts.Add(artifact);

        // 4. Link build to artifact
        var buildArtifact = new AppBuildArtifact
        {
            BuildJobId = buildId,
            ArtifactId = artifact.Id,
            Role = "primary"
        };
        appDbContext.AppBuildArtifacts.Add(buildArtifact);

        await appDbContext.SaveChangesAsync();

        logger.LogInformation("Uploaded artifact {ArtifactId} ({SizeBytes} bytes) for build {BuildId}", 
            artifact.Id, artifact.BundleSize, build.Id);

        return Ok(new 
        { 
            artifactId = artifact.Id,
            sizeBytes = artifact.BundleSize,
            contentHash = artifact.BundleHash
        });
    }
}