using Docker.DotNet;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;
using WebApp.Infrastructure.Repositories;
using WebApp.Gateway;
using WebApp.Domain.Models;
using WebApp.Gateway.Middlewares;
using WebApp.Gateway.Middlewares.Api;
using WebApp.Gateway.Middlewares.Spa;
using WebApp.Domain.Services;
using WebApp.Infrastructure.Storage;
using WebApp.Gateway.Cache;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(options =>
{
    options.AddSeq(builder.Configuration["Logging:Seq:ServerUrl"], builder.Configuration["Logging:Seq:ApiKey"]);
});
builder.Services.AddHttpLogging(_ => { });

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        //.LogTo(Console.WriteLine, LogLevel.Information)
        //.EnableSensitiveDataLogging()
        ;
});
builder.Services.AddScoped<IAppRepository, AppRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddHttpClient("HttpDocumentRetriever");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<ICachedOpenIdConnectionSigningKeys, CachedOpenIdConnectionSigningKeys>();
builder.Services.AddScoped<IAppSpecificationService, AppSpecificationService>();

var storagePath = builder.Configuration["Storage:RootPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "data");
builder.Services.AddSingleton<IStorageProvider>(new DiskStorageProvider(storagePath));

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
}, appBuilder => appBuilder.UseSpaStaticFileMiddleware());

app.UseWhen(context => 
{
    var route = context.Items["_RoutingConfigRoute"] as RoutingConfigRoute;
    return route?.Target.Type == RouteTargetType.Api;
}, appBuilder =>
{
    appBuilder.UseApiMiddleware();
});

app.Run();