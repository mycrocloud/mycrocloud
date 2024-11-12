using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nest;
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
    ElasticsearchClient elasticsearchClient)
    : BaseController
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
}