using System.Globalization;
using System.Text.Json;
using Jint;
using Jint.Native;

namespace WebApp.FunctionInvoker;

public class JintExecutor(SafeLogger logger)
{
    private readonly Engine _engine = new();

    private readonly List<string> _scripts =
    [
        "scripts/handlebars.min.js",
        "scripts/config.js"
    ];
    
    public void Initialize()
    {
        // Log
        _engine.SetValue("console", new
        {
            log = new Action<object?>(logger.Info),
            info = new Action<object?>(logger.Info),
            warn = new Action<object?>(logger.Warn),
            error = new Action<object?>(logger.Error)
        });

        // Env
        if (File.Exists("data/env.json"))
        {
            var envJson =  File.ReadAllText("data/env.json");
            var env = JsonSerializer.Deserialize<Dictionary<string, string>>(envJson);
            _engine.SetValue("env", env);
        }

        // Scripts
        foreach (var script in _scripts)
        {
            Console.WriteLine($"Loading script: {script}");
            var code = File.ReadAllText(script);
            _engine.Execute(code);
        }
        
        // String Values
        if (File.Exists("data/values.json"))
        {
            var valuesJson = File.ReadAllText("data/values.json");
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(valuesJson) ??
                         new Dictionary<string, string>();

            foreach (var (key, text) in values)
            {
                var name = key.Split(':')[0];
                var type = key.Split(':')[1];

                _engine.SetValue(name, text);

                switch (type)
                {
                    case "string":
                        break;
                    case "number":
                        _engine.Execute($"{name}=Number({name})");
                        break;
                    case "json":
                        _engine.Execute($"{name}=JSON.parse({name})");
                        break;
                }
            }
        }
        
        var requestJson = File.ReadAllText("data/request.json");
        var request = JsonSerializer.Deserialize<Request>(requestJson)!;
        
        _engine
            .SetValue("requestMethod", request.Method)
            .SetValue("requestPath", request.Path)
            .SetValue("requestParams", request.Params)
            .SetValue("requestQuery", request.Query)
            .SetValue("requestHeaders", request.Headers)
            .SetValue("bodyParser", "json") //TODO: make this dynamic?
            .SetValue("requestBody", request.Body)
            ;

        const string injectRequestCode =
            """
            const request = {
                method: requestMethod,
                path: requestPath,
                headers: requestHeaders,
                query: requestQuery,
                params: requestParams,
            };

            switch (bodyParser) {
                case "json":
                    request.body = requestBody ? JSON.parse(requestBody) : null;
                break;
            }
            """;
        _engine.Execute(injectRequestCode);
        
        var handler = File.ReadAllText("data/handler.js");
            
        _engine.Execute(handler);
    }

    public Result Execute()
    {
        var jsResult = _engine.Evaluate("(() => { return handler(request); })();");

        return GetResult(jsResult);
    }

    private static Result GetResult(JsValue jsValue)
    {
        var result = new Result();

        var statusCode = jsValue.Get("statusCode");
        if (statusCode.IsNumber())
        {
            result.StatusCode = (int)statusCode.AsNumber();
        }

        var headers = jsValue.Get("headers");
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

        var body = jsValue.Get("body");
        if (body.IsString())
        {
            result.Body = body.AsString();
        }

        return result;
    }
}