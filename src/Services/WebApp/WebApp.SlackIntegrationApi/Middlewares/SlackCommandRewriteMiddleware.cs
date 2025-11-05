using Microsoft.AspNetCore.WebUtilities;
using WebApp.SlackIntegrationApi.Extensions;

namespace WebApp.SlackIntegrationApi.Middlewares;

public class SlackCommandRewriteMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SlackCommandRewriteMiddleware> _logger;

    public SlackCommandRewriteMiddleware(RequestDelegate next, ILogger<SlackCommandRewriteMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.IsSlackCommandRequest())
        {
            var body = context.Items["Slack:Body"] as string;
            if (!string.IsNullOrEmpty(body))
            {
                var dict = QueryHelpers.ParseQuery(body);
                var text = dict.TryGetValue("text", out var val) ? val.ToString() : string.Empty;
                var cmd = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(cmd))
                {
                    _logger.LogDebug("Slack command rewrite: {Cmd}", cmd);
                    context.Request.Path = $"/slack/commands/{cmd}";
                }
            }
        }

        await _next(context);
    }
}

public static class SlackCommandRewriteMiddlewareExtensions
{
    public static IApplicationBuilder UseSlackCommandRewrite(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SlackCommandRewriteMiddleware>();
    }
}