using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Services;
using WebApp.Domain.Repositories;
using WebApp.Api.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using System.Reflection;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Nest;
using WebApp.Api.Authentications;
using WebApp.Infrastructure;
using WebApp.Infrastructure.Repositories;
using WebApp.Api.Filters;
using WebApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(GlobalExceptionFilter));
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
builder.Services.AddAuthentication("MultiAuthSchemes")
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Authentication:Schemes:Auth0JwtBearer:Authority"];
        options.Audience = builder.Configuration["Authentication:Schemes:Auth0JwtBearer:Audience"];
        
        //for log streaming over SSE
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    })
    .AddScheme<ApiTokenAuthenticationOptions, ApiTokenAuthenticationHandler>(ApiTokenDefaults.AuthenticationScheme, options => { })
    .AddPolicyScheme("MultiAuthSchemes", displayName: null, options =>
    {
        //TODO: re-think
        options.ForwardDefaultSelector = ctx => ctx.Request.Host.Host.StartsWith("api")
            ? ApiTokenDefaults.AuthenticationScheme
            : JwtBearerDefaults.AuthenticationScheme;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("M2M", policy =>
    {
        policy.RequireClaim("gty", "client-credentials");
    });
});

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
builder.Services.AddHostedService<AppBuildStatusConsumer>();
builder.Services.AddSingleton<IAppBuildPublisher, InMemoryAppBuildPublisher>();

builder.Services.AddKeyedSingleton("AppBuildLogs_ES7", (_, _) =>
{
    var settings = new ConnectionSettings(new Uri(builder.Configuration["Elasticsearch:Host"]!))
        .BasicAuthentication(builder.Configuration["Elasticsearch:Username"]!,
            builder.Configuration["Elasticsearch:Password"]!)
        .DefaultIndex(builder.Configuration["Elasticsearch:BuildLogsIndex"]!);

    return new ElasticClient(settings);
});

builder.Services.AddKeyedSingleton("AppBuildLogs_ES8", (_, _) =>
{
    var settings = new ElasticsearchClientSettings(new Uri(builder.Configuration["Elasticsearch:Host"]!))
        .Authentication(new BasicAuthentication(builder.Configuration["Elasticsearch:Username"]!,
            builder.Configuration["Elasticsearch:Password"]!))
        .DefaultIndex(builder.Configuration["Elasticsearch:BuildLogsIndex"]!);

    return new ElasticsearchClient(settings);
});

builder.Services.Configure<GitHubAppOptions>(builder.Configuration.GetSection("ExternalIntegrations:GitHubApp"));
builder.Services.AddHttpClient<GitHubAppService>(client =>
{
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MycroCloud", "1.0.0"));
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
});

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
    var options = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    };

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
    
    app.UseForwardedHeaders(options);
    
    app.UsePathBase("/api");
}

var behindProxy = builder.Configuration.GetValue<bool>("ASPNETCORE_BEHIND_PROXY");

if (behindProxy)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
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
app.Map("ping", () => "pong");
app.Map("me", (ClaimsPrincipal user) =>
    {
        var userId = user.GetUserId();

        return Task.FromResult(new
        {
            userId,
        });
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