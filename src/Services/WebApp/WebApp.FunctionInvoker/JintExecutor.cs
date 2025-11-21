using System.Collections;
using System.Globalization;
using Jint;
using Jint.Native;

namespace WebApp.FunctionInvoker;

public class JintExecutor(SafeLogger logger)
{
    private readonly Engine _engine = new();

    private readonly List<string> _reservedEnv = [];

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
                continue;
            }
            
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
    }

    public Result Execute(string handler, Request request)
    {
        _engine.SetRequestValue(request);

        _engine.Execute(handler);

        var jsResult = _engine.Evaluate("(() => { return handler(request); })();");

        return Map(jsResult);
    }

    private static Result Map(JsValue jsResult)
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