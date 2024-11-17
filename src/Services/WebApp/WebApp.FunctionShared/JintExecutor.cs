using System.Globalization;
using Jint;
using Jint.Native;

namespace WebApp.FunctionShared;

public class JintExecutor(Engine engine) : IExecutor
{
    public JintExecutor() : this(new Engine())
    {
    }

    public Result Execute(Request request, string handler)
    {
        engine.SetRequestValue(request);

        engine.Execute(handler);

        var jsResult = engine.Evaluate("(() => { return handler(request); })();");

        return Map(jsResult);
    }

    public static Result Map(JsValue jsResult)
    {
        var result = new Result();
        
        var statusCode = jsResult.Get("statusCode");
        if (statusCode.IsNumber())
        {
            result.StatusCode = (int)statusCode.AsNumber();
        }

        var headers = jsResult.Get("headers");
        if (headers.IsObject())
        {
            var headersObject = headers.AsObject();
            var headersObjectProperties = headersObject.GetOwnProperties();
            foreach (var (k, v) in headersObjectProperties)
            {
                var headerName = k.AsString();
                string headerValue;

                var value = v.Value;

                if (value.IsNull())
                {
                    continue;
                }

                if (value.IsNumber())
                {
                    headerValue = value.AsNumber().ToString(CultureInfo.InvariantCulture);
                }
                else if (value.IsString())
                {
                    headerValue = value.AsString();
                }
                else if (value.IsBoolean())
                {
                    headerValue = value.AsBoolean().ToString();
                }
                else
                {
                    continue;
                }

                if (!result.Headers.TryAdd(headerName, headerValue))
                {
                    result.Headers[headerName] = headerValue;
                }
            }
        }

        var body = jsResult.Get("body");
        if (body.IsString())
        {
            result.Body = body.AsString();
        }

        return result;
    }
}