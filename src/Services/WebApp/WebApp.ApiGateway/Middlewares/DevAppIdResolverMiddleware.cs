namespace WebApp.ApiGateway.Middlewares;

public class DevAppIdResolverMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var host = context.Request.Host.Host;
        var pattern = configuration["HostRegex"]!;
        var match = System.Text.RegularExpressions.Regex.Match(host, pattern);
        if (match.Success)
        {
            var appId = int.Parse(match.Groups[1].Value);
            var source = configuration["AppIdSource"]!.Split(":")[0];
            var name = configuration["AppIdSource"]!.Split(":")[1];
            if (source == "Header")
            {
                context.Request.Headers.Append(name, appId.ToString());
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