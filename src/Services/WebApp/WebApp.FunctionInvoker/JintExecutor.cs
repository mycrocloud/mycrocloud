using System.Globalization;
using System.Text.Json;
using Jint;
using Jint.Native;
using WebApp.FunctionInvoker.Apis.Fetch;
using Console = WebApp.FunctionInvoker.Apis.Console.Console;

namespace WebApp.FunctionInvoker;

public class JintExecutor
{
    private readonly Engine _engine = new();
    public Console Console { get; set; }

    private readonly List<string> _scripts =
    [
        "scripts/handlebars.min.js",
        "scripts/init.js"
    ];
    
    public void Initialize()
    {
        InstallApis();

        // Env
        if (File.Exists("data/env.json"))
        {
            const string env = "env";
            var envJson =  File.ReadAllText($"data/{env}.json");
            _engine.SetValue(env, envJson);
            _engine.Execute($"{env}=JSON.parse({env})");
        }

        // Scripts
        foreach (var script in _scripts)
        {
            System.Console.WriteLine($"Loading script: {script}");
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

    private void InstallApis()
    {
        // Log
        Console = new Console("data/log.json");
        _engine.SetValue("console", new
        {
            log = new Action<object?>(Console.Info),
            info = new Action<object?>(Console.Info),
            warn = new Action<object?>(Console.Warn),
            error = new Action<object?>(Console.Error)
        });
        
        // Fetch
        var proxyFetch = new FetchProxy(50);
        _engine.SetValue("fetch", new Func<JsValue, JsValue?, JsValue>((input, init) =>
        {
            var request = Mapper.MapRequest(input, init);
            
            var response = proxyFetch.Fetch(request).Result;
            
            return Mapper.MapResponse(response);
        }));
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