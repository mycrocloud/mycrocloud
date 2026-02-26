using Docker.DotNet;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Amazon.S3;
using MycroCloud.WebApp.Gateway.Middlewares;
using MycroCloud.WebApp.Gateway.Middlewares.Api;
using MycroCloud.WebApp.Gateway.Middlewares.Spa;
using MycroCloud.WebApp.Gateway.Models;
using MycroCloud.WebApp.Gateway.Services;
using MycroCloud.WebApp.Gateway.Utils;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(options =>
{
    options.AddSeq(builder.Configuration["Logging:Seq:ServerUrl"]!, builder.Configuration["Logging:Seq:ApiKey"]);
});
builder.Services.AddHttpLogging(_ => { });

var npgsqlDataSource = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection")).Build();
builder.Services.AddSingleton(npgsqlDataSource);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(npgsqlDataSource);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            ;
    }
});
builder.Services.AddSingleton<AccessLogChannel>();
builder.Services.AddHostedService<AccessLogBackgroundService>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("HttpDocumentRetriever");
// Distributed cache backed by PostgreSQL UNLOGGED table
builder.Services.AddSingleton<IDistributedCache>(sp => new PostgresCacheService(sp.GetRequiredService<NpgsqlDataSource>()));
builder.Services.AddHostedService<CacheCleanupService>();
builder.Services.AddSingleton<ICachedOpenIdConnectionSigningKeys, CachedOpenIdConnectionSigningKeys>();
builder.Services.AddScoped<IAppSpecificationService, AppSpecificationService>();

// Storage Provider selection (Disk or S3/R2)
var storageType = builder.Configuration["Storage:Type"] ?? "Disk";
if (storageType.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    var s3Config = new AmazonS3Config
    {
        ServiceURL = builder.Configuration["Storage:S3:ServiceURL"],
        ForcePathStyle = true
    };
    var s3Client = new AmazonS3Client(
        builder.Configuration["Storage:S3:AccessKey"],
        builder.Configuration["Storage:S3:SecretKey"],
        s3Config);

    builder.Services.AddSingleton<IAmazonS3>(s3Client);
    builder.Services.AddSingleton<IStorageProvider>(new S3StorageProvider(s3Client, builder.Configuration["Storage:S3:BucketName"]!));
}

builder.Services.AddKeyedSingleton("DockerFunctionExecution", new ConcurrentJobQueue(maxConcurrency: 100));
builder.Services.AddSingleton(_ =>
{
    var client = new DockerClientConfiguration(
            new Uri(builder.Configuration["DockerFunctionExecution:Uri"]!))
        .CreateClient();

    return client;
});
// Function executors - add new IFunctionExecutor implementations here
builder.Services.AddScoped<IFunctionExecutor, DockerFunctionExecutor>();
builder.Services.AddScoped<FunctionExecutorFactory>();

// Response handlers - add new IResponseHandler implementations here
builder.Services.AddScoped<IResponseHandler, StaticResponseHandler>();
builder.Services.AddScoped<IResponseHandler, FunctionResponseHandler>();

builder.Services.AddHealthChecks();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    var options = new ForwardedHeadersOptions
    {
        ForwardedHeaders = 
            ForwardedHeaders.XForwardedFor | 
            ForwardedHeaders.XForwardedHost |
            ForwardedHeaders.XForwardedProto
    };
    
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    
    options.KnownNetworks.Add(app.Configuration["Proxy:Subnet"]!.ParseCidr());
    
    options.ForwardLimit = null;    //TODO: what is this?
    options.RequireHeaderSymmetry = false; //TODO: what is this?
    
    app.UseForwardedHeaders(options);
}
app.UseHttpLogging();
app.UseWhen(context => context.Request.Host.Host == builder.Configuration["Host"], config =>
{
    config.UseHealthChecks("/healthz");
    
    // short-circuit the pipeline here
    config.Run(async context => { await context.Response.CompleteAsync(); });
});

app.UseLoggingMiddleware();

app.UseAppResolverMiddleware();

app.UseRoutingMiddleware();

app.UseWhen(context => 
{
    var route = context.Items["_RoutingConfigRoute"] as RoutingConfigRoute;
    return route?.Target.Type == RouteTargetType.Static;
}, appBuilder => appBuilder.UseSpaMiddleware());

app.UseWhen(context => 
{
    var route = context.Items["_RoutingConfigRoute"] as RoutingConfigRoute;
    return route?.Target.Type == RouteTargetType.Api;
}, appBuilder => appBuilder.UseApiMiddleware());

app.Run();
