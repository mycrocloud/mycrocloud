namespace WebApp.ApiGateway.Middlewares;

public class DevAppNameResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var host = context.Request.Host.Host;
        var pattern = configuration["HostRegex"]!;
        var match = System.Text.RegularExpressions.Regex.Match(host, pattern);
        if (match.Success)
        {
            var appName = match.Groups[1].Value;
            var source = configuration["AppNameSource"]!.Split(":")[0];
            var headerName = configuration["AppNameSource"]!.Split(":")[1];
            if (source == "Header")
            {
                context.Request.Headers.Append(headerName, appName);
            }
            await next(context);
        }
        else
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid host");
        }
    }
}