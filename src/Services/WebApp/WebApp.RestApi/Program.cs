using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApp.RestApi;
using WebApp.Domain.Services;
using WebApp.Domain.Repositories;
using WebApp.RestApi.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Nest;
using WebApp.Infrastructure;
using WebApp.Infrastructure.Repositories;
using WebApp.RestApi.Filters;
using WebApp.RestApi.Hubs;
using WebApp.RestApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(GlobalExceptionFilter));
    options.InputFormatters.Insert(options.InputFormatters.Count, new TextPlainInputFormatter());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(options =>
{
    options.AddSeq(builder.Configuration["Logging:Seq:ServerUrl"], builder.Configuration["Logging:Seq:ApiKey"]);
});
builder.Services.AddHttpLogging(_ => { });
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]!.Split(','))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 1. Add Authentication Services
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Schemes:Auth0JwtBearer:Authority"];
        options.Audience = builder.Configuration["Authentication:Schemes:Auth0JwtBearer:Audience"];
    });
builder.Services.AddAuthorization();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<IAppRepository, AppRepository>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<AppBuildJobStatusConsumer>();
builder.Services.AddKeyedSingleton<ElasticClient>("AppBuildLogs_ES7", (_, _) =>
{
    var settings = new ConnectionSettings(new Uri(builder.Configuration["Elasticsearch:Host"]!))
        .BasicAuthentication(builder.Configuration["Elasticsearch:Username"]!,
            builder.Configuration["Elasticsearch:Password"]!)
        .DefaultIndex(builder.Configuration["Elasticsearch:BuildLogsIndex"]!);

    return new ElasticClient(settings);
});

builder.Services.AddKeyedSingleton<ElasticsearchClient>("AppBuildLogs_ES8", (_, _) =>
{
    var settings = new ElasticsearchClientSettings(new Uri(builder.Configuration["Elasticsearch:Host"]!))
        .Authentication(new BasicAuthentication(builder.Configuration["Elasticsearch:Username"]!,
            builder.Configuration["Elasticsearch:Password"]!))
        .DefaultIndex(builder.Configuration["Elasticsearch:BuildLogsIndex"]!);

    return new ElasticsearchClient(settings);
});

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("_config", () => builder.Configuration.GetDebugView());
}

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}


app.UseHttpLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapHub<TestHub>("/functionExecutionHub");
app.Map("ping", () => "pong");
app.Map("me", async (AppDbContext appDbContext, ClaimsPrincipal user) =>
    {
        var userId = user.GetUserId();
        var tokens = await appDbContext.UserTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();

        return new
        {
            userId,
            connections = tokens.Select(t => new { t.Provider, t.Purpose, t.CreatedAt, t.UpdatedAt })
        };
    })
    .RequireAuthorization();

app.MapGet("_assembly", () =>
{
    var assembly = Assembly.GetExecutingAssembly();
    return new
    {
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    };
}).RequireAuthorization();

app.Run();