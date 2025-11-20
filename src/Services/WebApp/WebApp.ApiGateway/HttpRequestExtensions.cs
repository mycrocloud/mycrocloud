using Microsoft.AspNetCore.Cors.Infrastructure;
using WebApp.FunctionShared;

namespace WebApp.ApiGateway;

public static class HttpRequestExtensions
{
    public static bool IsPreflightRequest(this HttpRequest request)
    {
        return HttpMethods.IsOptions(request.Method)
               && request.Headers.ContainsKey(CorsConstants.AccessControlRequestMethod);
    }

    public static async Task<Request> Normalize(this HttpRequest request)
    {
        return new Request
        {
            Method = request.Method,
            Path = request.Path.Value,
            Params = request.RouteValues.ToDictionary(x => x.Key, x => x.Value?.ToString()),
            Query = request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Headers = request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
            Body = await new StreamReader(request.Body).ReadToEndAsync(),
        };
    }
    public static string? Evaluate(this HttpRequest request, string expression)
    {
        var parts = expression.Split(':', 2);
        if (parts.Length != 2) return null;

        var source = parts[0];
        var key = parts[1];

        if (source.Equals("Header", StringComparison.OrdinalIgnoreCase))
        {
            if (request.Headers.TryGetValue(key, out var value))
            {
                return value.ToString();
            }
        }

        return null;
    }
}