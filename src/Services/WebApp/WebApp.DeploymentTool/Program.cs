using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Services;
using Api.Infrastructure;
using Api.Infrastructure.Services;
using Api.Infrastructure.Storage;
using Amazon.S3;

var builder = Host.CreateApplicationBuilder(args);

// 1. Add Infrastructure
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// 2. Add Storage (Disk or S3/R2)
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
else
{
    var storagePath = builder.Configuration["Storage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "data");
    builder.Services.AddSingleton<IStorageProvider>(new DiskStorageProvider(storagePath));
}

// 3. Add Services
builder.Services.AddScoped<IAppSpecificationPublisher, AppSpecificationPublisher>();
builder.Services.AddScoped<IApiDeploymentService, ApiDeploymentService>();
builder.Services.AddScoped<IOpenApiGenerator, OpenApiGenerator>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var apiDeploymentService = host.Services.GetRequiredService<IApiDeploymentService>();

logger.LogInformation("Starting Deployment Bootstrap Tool...");

try
{
    var count = await apiDeploymentService.BootstrapLegacyAppsAsync();
    logger.LogInformation("Successfully bootstrapped {Count} legacy apps.", count);
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Bootstrap process failed.");
}

await host.StopAsync();
