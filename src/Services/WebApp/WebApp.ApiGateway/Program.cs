using Docker.DotNet;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;
using WebApp.Infrastructure.Repositories;
using WebApp.ApiGateway;
using WebApp.ApiGateway.Middlewares;
using File = System.IO.File;

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
builder.Services.AddSingleton(new Scripts
{
    Handlebars = File.ReadAllText("Scripts/handlebars.min-v4.7.8.js"),
});
builder.Services.AddHttpClient("HttpDocumentRetriever");
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<ICachedOpenIdConnectionSigningKeys, CachedOpenIdConnectionSigningKeys>();

builder.Services.AddKeyedSingleton("InProcessFunctionExecutionManager", new ConcurrencyJobManager(100));
builder.Services.AddKeyedSingleton("DockerContainerFunctionExecutionManager", new ConcurrencyJobManager(100));
builder.Services.AddSingleton(_ =>
{
    var client = new DockerClientConfiguration(
            new Uri(builder.Configuration["DockerFunctionExecution:Uri"]!))
        .CreateClient();

    return client;
});
builder.Services.AddHealthChecks();

var app = builder.Build();
app.UseRouting();
app.UseHttpLogging();
app.UseWhen(context => context.Request.Host.Host == builder.Configuration["Host"], config =>
{
    config.UseHealthChecks("/healthz");
    
    // short-circuit the pipeline here
    config.Run(async context => { await context.Response.CompleteAsync(); });
});

app.UseLoggingMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<DevAppIdResolverMiddleware>();
}

app.UseAppResolverMiddleware();

app.UseStaticFilesMiddleware2();

app.UseCorsMiddleware();

app.UseRouteResolverMiddleware();

app.UseAuthenticationMiddleware();

app.UseAuthorizationMiddleware();

app.UseValidationMiddleware();

app.UseWhen(context => ((Route)context.Items["_Route"]!).ResponseType == ResponseType.Static,
    appBuilder => appBuilder.UseStaticResponseMiddleware());

app.UseWhen(context => ((Route)context.Items["_Route"]!).ResponseType == ResponseType.StaticFile,
    appBuilder => appBuilder.UseStaticFilesMiddleware());

app.UseWhen(context => ((Route)context.Items["_Route"]!).ResponseType == ResponseType.Function,
    appBuilder => appBuilder.UseFunctionInvokerMiddleware());

app.Run();