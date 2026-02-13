using WebApp.Gateway.Models;

namespace WebApp.Gateway.Utils;

public static class HttpResponseExtensions
{
    public static Task WriteNotFound(this HttpResponse response, string text)
    {
        response.StatusCode = 404;
        return response.WriteAsync(text);
    }
    
    public static Task WriteFromFunctionResult(this HttpResponse response, FunctionResult result)
    {
        response.StatusCode = result.StatusCode!.Value;
        
        response.Headers.Clear();

        foreach (var kv in result.Headers)
        {
            if (!string.IsNullOrEmpty(kv.Key) && !string.IsNullOrEmpty(kv.Value))
            {
                response.Headers.Append(kv.Key, kv.Value);
            }
        }
        
        return response.WriteAsync(result.Body!);
    }
}