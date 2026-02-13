using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Services;
using Api.Domain.Repositories;
using Api.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Api.Authentications;
using Api.Infrastructure;
using Api.Infrastructure.Repositories;
using Api.Middlewares;
using Api.Services;
using Api.Infrastructure.Storage;
using Api.Infrastructure.Services;
using Amazon.S3;

DotNetEnv.Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

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
    .AddScheme<SlackAuthenticationOptions, SlackAuthenticationHandler>(SlackDefaults.AuthenticationScheme, _ => { })
    .AddPolicyScheme(Constants.MultiAuthSchemes, displayName: null, options =>
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

var npgsqlDataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
npgsqlDataSourceBuilder.EnableDynamicJson();
var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(npgsqlDataSource);
});
builder.Services.AddScoped<IAppRepository, AppRepository>();
builder.Services.AddScoped<IAppService, AppService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddSingleton<RabbitMqService>();
builder.Services.AddHttpClient();
builder.Services.AddHostedService<AppBuildStatusConsumer>();
builder.Services.AddSingleton<IAppBuildPublisher, InMemoryAppBuildPublisher>();
builder.Services.AddScoped<SlackAppService>();
builder.Services.AddHostedService<SubscribeService>();

// Redis cache for Gateway cache invalidation
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddScoped<IAppCacheInvalidator, AppCacheInvalidator>();
builder.Services.AddScoped<IAppSpecificationPublisher, AppSpecificationPublisher>();
builder.Services.AddScoped<IArtifactExtractionService, ArtifactExtractionService>();
builder.Services.AddScoped<IApiDeploymentService, ApiDeploymentService>();
builder.Services.AddScoped<IOpenApiGenerator, OpenApiGenerator>();
builder.Services.AddScoped<BuildOrchestrationService>();

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
else
{
    var storagePath = builder.Configuration["Storage:RootPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "data");
    builder.Services.AddSingleton<IStorageProvider>(new DiskStorageProvider(storagePath));
}

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
}

var behindProxy = builder.Configuration.GetValue<bool>("ASPNETCORE_BEHIND_PROXY");

if (behindProxy)
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}

app.UseGlobalExceptionHandler();

app.UseHttpLogging();
app.UseCors();

app.UseMiddleware<ReadSlackRequestBodyMiddleware>();
app.UseSlackVerification();
app.UseSlackCommandRewrite();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/healthz");
app.Map("me", (ClaimsPrincipal user) =>
    {
        var userId = user.GetUserId();

        return Task.FromResult(new
        {
            userId,
        });
    })
    .RequireAuthorization();

app.UseSlackCommandFallback();
app.Run();