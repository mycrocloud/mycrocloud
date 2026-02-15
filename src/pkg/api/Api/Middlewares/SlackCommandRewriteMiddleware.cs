using Microsoft.AspNetCore.WebUtilities;
using Api.Extensions;

namespace Api.Middlewares;

public class SlackCommandRewriteMiddleware(RequestDelegate next, ILogger<SlackCommandRewriteMiddleware> logger)
{
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
                    logger.LogDebug("Slack command rewrite: {Cmd}", cmd);
                    context.Request.Path = $"/slack/commands/{cmd}";
                }
            }
        }

        await next(context);
    }
}

public static class SlackCommandRewriteMiddlewareExtensions
{
    public static IApplicationBuilder UseSlackCommandRewrite(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SlackCommandRewriteMiddleware>();
    }
}