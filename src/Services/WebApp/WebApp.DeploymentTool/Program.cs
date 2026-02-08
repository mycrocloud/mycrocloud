using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Services;
using WebApp.Infrastructure;
using WebApp.Infrastructure.Services;
using WebApp.Infrastructure.Storage;

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

// 2. Add Storage
var storagePath = builder.Configuration["Storage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "data");
builder.Services.AddSingleton<IStorageProvider>(new DiskStorageProvider(storagePath));

// 3. Add Services
builder.Services.AddScoped<IAppSpecificationPublisher, AppSpecificationPublisher>();
builder.Services.AddScoped<IApiDeploymentService, ApiDeploymentService>();

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
