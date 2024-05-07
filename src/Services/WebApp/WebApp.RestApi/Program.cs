using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApp.RestApi;
using WebApp.Domain.Services;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure.Repositories.EfCore;
using WebApp.RestApi.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using System.Reflection;
using WebApp.RestApi.Filters;

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
app.Map("ping", () => "pong");
app.Map("me", (ClaimsPrincipal user) => user.GetUserId())
    .RequireAuthorization();

app.MapGet("_assembly", ()  => {
    var assembly = Assembly.GetExecutingAssembly();
    return new
    {
        assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    };
}).RequireAuthorization();

app.Run();