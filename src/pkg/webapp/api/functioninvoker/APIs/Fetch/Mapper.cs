using Jint;
using Jint.Native;

namespace WebApp.FunctionInvoker.Apis.Fetch;

public static class Mapper
{
    private static readonly HashSet<string> ContentHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Content-Type", "Content-Length", "Content-Encoding", "Content-Language",
        "Content-Location", "Content-Range", "Content-Disposition"
    };

    private static readonly HashSet<string> ForbiddenRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host", "Cookie", "Set-Cookie"
    };

    public static HttpRequestMessage MapRequest(JsValue input, JsValue? init = null)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get
        };

        if (input.IsString())
        {
            request.RequestUri = new Uri(input.AsString());
        }

        if (init is null || init.IsUndefined() || init.IsNull())
            return request;

        // Method
        var method = init.Get("method");
        if (method.IsString())
        {
            request.Method = HttpMethod.Parse(method.AsString());
        }

        // Headers
        string? contentType = null;
        var headers = init.Get("headers");
        if (headers.IsObject())
        {
            var headersObj = headers.AsObject();
            foreach (var (key, descriptor) in headersObj.GetOwnProperties())
            {
                var name = key.AsString();
                var value = descriptor.Value;
                if (value.IsUndefined() || value.IsNull())
                    continue;

                var headerValue = value.AsString();

                if (ForbiddenRequestHeaders.Contains(name))
                    continue;

                if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = headerValue;
                    continue;
                }

                if (ContentHeaders.Contains(name))
                    continue; // other content headers set via StringContent

                request.Headers.TryAddWithoutValidation(name, headerValue);
            }
        }

        // Body
        var body = init.Get("body");
        if (body.IsString())
        {
            var mediaType = contentType ?? "text/plain";
            request.Content = new StringContent(body.AsString(), System.Text.Encoding.UTF8, mediaType);
        }

        return request;
    }

    public static JsValue MapResponse(HttpResponseMessage response, Engine engine)
    {
        var body = response.Content.ReadAsStringAsync().Result;
        var statusCode = (int)response.StatusCode;

        // Build headers dictionary
        var headerDict = new Dictionary<string, string>();
        foreach (var header in response.Headers)
        {
            headerDict[header.Key.ToLowerInvariant()] = string.Join(", ", header.Value);
        }
        if (response.Content.Headers is { } contentHeaders)
        {
            foreach (var header in contentHeaders)
            {
                headerDict[header.Key.ToLowerInvariant()] = string.Join(", ", header.Value);
            }
        }

        // Use anonymous object → Jint wraps it as a JS object
        var obj = JsValue.FromObject(engine, new
        {
            status = statusCode,
            statusText = response.ReasonPhrase ?? "",
            ok = statusCode >= 200 && statusCode <= 299,
            headers = headerDict,
            text = new Func<JsValue>(() => new JsString(body)),
            json = new Func<JsValue>(() =>
            {
                var jsonLiteral = System.Text.Json.JsonSerializer.Serialize(body);
                return engine.Evaluate($"JSON.parse({jsonLiteral})");
            })
        });

        return obj;
    }
}
