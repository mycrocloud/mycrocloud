using WebApp.SlackIntegration.Extensions;

namespace WebApp.SlackIntegration.Middlewares;

public class ReadSlackRequestBodyMiddleware(RequestDelegate next, ILogger<ReadSlackRequestBodyMiddleware> logger)
{
    private readonly ILogger _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.IsSlackCommandRequest())
        {
            var requestBody = await context.ReadRequestBody();
            
            _logger.LogDebug("Request Body:{@requestBody}", requestBody);
            
            context.Items.Add("Slack:Body", requestBody);
        }

        await next(context);
    }
}