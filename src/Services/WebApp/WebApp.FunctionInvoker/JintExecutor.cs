using System.Collections;
using System.Globalization;
using Jint;
using Jint.Native;

namespace WebApp.FunctionInvoker;

public class JintExecutor(SafeLogger logger)
{
    private readonly Engine _engine = new();

    private readonly List<string> _reservedEnv = [
        "HOSTNAME",
        "HOME",
        "DOTNET_RUNNING_IN_CONTAINER",
        "PATH",
        "DOTNET_VERSION",
        "ASPNETCORE_HTTP_PORTS",
        "APP_UID"
    ];

    public void Initialize()
    {
        // Log
        _engine.SetValue("console", new
        {
            log = new Action<object?>(logger.Log),
            info = new Action<object?>(logger.Log),
            warn = new Action<object?>(logger.Log),
            error = new Action<object?>(logger.Log)
        });

        // Env
        var env = new Dictionary<string, string>();

        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            if (_reservedEnv.Contains(de.Key.ToString()!))
            {
                // ignore system reserved env
                Console.WriteLine($"Environment variable '{de.Key}' is reserved.");
                continue;
            }
            Console.WriteLine($"Inject env. {de.Key} = {de.Value}");
            env[de.Key.ToString()!] = de.Value?.ToString() ?? "";
        }

        _engine.SetValue("env", env);

        // Scripts
        var scripts = Directory.GetFiles("scripts")
            .OrderBy(f => f)
            .ToList();

        foreach (var script in scripts)
        {
            Console.WriteLine($"Loading script: {script}");
            var code = File.ReadAllText(script);
            _engine.Execute(code);
        }
        
        // String Values
        if (Directory.Exists("data/string_values"))
        {
            var stringValues = Directory.GetFiles("data/string_values")
                .OrderBy(f => f)
                .ToList();
        
            foreach (var file in stringValues)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var value = File.ReadAllText(file);
                
                Console.WriteLine($"Loading string: {name}");
                _engine.SetValue(name, value);
            }
        }
    }

    public Result Execute(string handler, Request request)
    {
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

        _engine.Execute(handler);

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