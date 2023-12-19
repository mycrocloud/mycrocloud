using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApp.Api.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
{
    options.Filters.Add(typeof(GlobalExceptionFilter));
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(options => {
    options.AddSeq(builder.Configuration["Logging:Seq:ServerUrl"]);
});
builder.Services.AddHttpLogging(o => { });
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// 1. Add Authentication Services
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.Authority = "https://dev-vzxphouz.us.auth0.com/";
    options.Audience = "https://mycrocloud.com";
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseHttpLogging();
app.MapHealthChecks("/healthz");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Map("api/ping", () => "pong");
app.Map("api/me", (ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier)!)
    .RequireAuthorization();

app.Run();