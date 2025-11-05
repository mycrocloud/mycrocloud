using System.Text.Json;
using WebApp.SlackIntegrationApi.Extensions;

namespace WebApp.SlackIntegrationApi.Middlewares;

public static class SlackFallbackExtensions
{
    public static IApplicationBuilder UseSlackCommandFallback(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == 404 &&
                context.Request.IsSlackCommandRequest())
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";

                var command = context.Request.Path.Value?.Split('/').Last();
                var json = JsonSerializer.Serialize(new
                {
                    response_type = "ephemeral",
                    text = $"⚠️ Command `{command}` not found. Try `/mycrocloud help`."
                });

                await context.Response.WriteAsync(json);
            }
        });
    }
}
