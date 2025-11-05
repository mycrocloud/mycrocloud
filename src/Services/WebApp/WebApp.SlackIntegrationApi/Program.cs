using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure;
using WebApp.SlackIntegrationApi.Authentication;
using WebApp.SlackIntegrationApi.Middlewares;
using WebApp.SlackIntegrationApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddAuthentication()
    .AddScheme<SlackAuthenticationOptions, SlackAuthenticationHandler>(SlackDefaults.AuthenticationScheme, _ => { })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = builder.Configuration["Authentication:Schemes:Auth0JwtBearer:Authority"];
        options.Audience = builder.Configuration["Authentication:Schemes:Auth0JwtBearer:Audience"];
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["WebOrigin"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddScoped<SlackAppService>();
builder.Services.AddHostedService<SubscribeService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    });
}

app.UseMiddleware<ReadSlackRequestBodyMiddleware>();
app.UseSlackVerification();
app.UseSlackCommandRewrite();

app.UseRouting();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ping", () => "pong");
app.MapGet("/__app_echo", (HttpContext ctx) =>
{
    var hdr = ctx.Request.Headers;
    return Results.Text($@"
Request.Scheme={ctx.Request.Scheme}
Request.Host={ctx.Request.Host}
X-Forwarded-Proto={hdr["X-Forwarded-Proto"].FirstOrDefault()}
X-Forwarded-Host={hdr["X-Forwarded-Host"].FirstOrDefault()}
HostHeader={hdr["Host"].FirstOrDefault()}
X-Forwarded-For={hdr["X-Forwarded-For"].FirstOrDefault()}
", "text/plain");
});

app.MapControllers();
app.UseSlackCommandFallback();

app.Run();