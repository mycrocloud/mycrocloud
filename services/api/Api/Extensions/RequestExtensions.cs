using System.Text;

namespace Api.Extensions;

public static class RequestExtensions
{
    /// <summary>
    /// TODO: make this more maintainable
    /// </summary>
    public static async Task<string> ReadRequestBody(this HttpContext context)
    {
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
            
        return body;
    }

    public static bool IsSlackCommandRequest(this HttpRequest request)
    {
        return request.Path.StartsWithSegments("/slack/commands") &&
               request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase);
    }
}