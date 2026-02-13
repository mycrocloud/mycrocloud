using System.Security.Cryptography;
using System.Text;
using Api.Extensions;

namespace Api.Middlewares;
public class SlackVerificationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, IConfiguration configuration, IHostEnvironment environment, ILogger<SlackVerificationMiddleware> logger)
    {
        // Always check in production
        
        if (environment.IsDevelopment())
        {
            var verifySlackRequest =
                !bool.TryParse(configuration["VerifySlackRequest"], out var verify) || verify;
            
            if (!verifySlackRequest)
            {
                await next(context);
                return;
            }
        }

        if (context.Request.IsSlackCommandRequest())
        {
            if (!context.Request.HasFormContentType ||
                !context.Request.ContentType.StartsWith("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid content type. Expected application/x-www-form-urlencoded");
                return;
            }

            var slackSigningSecret = configuration.GetSection("ExternalIntegrations:Slack")["SigningSecret"];
            if (string.IsNullOrEmpty(slackSigningSecret))
            {
                logger.LogError("Missing Slack signing secret");
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Missing Slack signing secret");
                return;
            }

            var signature = context.Request.Headers["X-Slack-Signature"].FirstOrDefault();
            var timestamp = context.Request.Headers["X-Slack-Request-Timestamp"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(timestamp))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing Slack signature headers");
                return;
            }

            if (!long.TryParse(timestamp, out var ts))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Invalid timestamp");
                return;
            }

            var reqTime = DateTimeOffset.FromUnixTimeSeconds(ts);
            if (Math.Abs((DateTimeOffset.UtcNow - reqTime).TotalMinutes) > 5)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Timestamp too old");
                return;
            }

            var body = context.Items["Slack:Body"] as string;

            var baseString = $"v0:{timestamp}:{body}";
            var keyBytes = Encoding.UTF8.GetBytes(slackSigningSecret);
            var msgBytes = Encoding.UTF8.GetBytes(baseString);

            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(msgBytes);
            var hash = "v0=" + BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(hash),
                Encoding.UTF8.GetBytes(signature)))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid Slack signature");
                return;
            }
        }

        await next(context);
    }
}

public static class SlackVerificationMiddlewareExtensions
{
    public static IApplicationBuilder UseSlackVerification(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SlackVerificationMiddleware>();
    }
}