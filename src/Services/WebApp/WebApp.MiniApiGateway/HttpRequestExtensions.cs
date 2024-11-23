using Microsoft.AspNetCore.Cors.Infrastructure;
using WebApp.FunctionShared;

namespace WebApp.MiniApiGateway;

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
}