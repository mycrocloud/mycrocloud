using Microsoft.AspNetCore.Cors.Infrastructure;
using WebApp.FunctionShared;

namespace WebApp.ApiGateway;

public static class HttpResponseExtensions
{
    public static Task WriteNotFound(this HttpResponse response, string text)
    {
        response.StatusCode = 404;
        return response.WriteAsync(text);
    }
}