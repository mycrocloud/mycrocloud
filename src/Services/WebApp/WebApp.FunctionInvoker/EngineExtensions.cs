using Jint;

namespace WebApp.FunctionInvoker;

public static class EngineExtensions
{
    public static void SetRequestValue(this Engine engine, Request request)
    {
        engine
            .SetValue("requestMethod", request.Method)
            .SetValue("requestPath", request.Path)
            .SetValue("requestParams", request.Params)
            .SetValue("requestQuery", request.Query)
            .SetValue("requestHeaders", request.Headers)
            .SetValue("bodyParser", "json")
            .SetValue("requestBody", request.Body)
            ;

        const string code = """
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

        engine.Execute(code);
    }

    public static void SetEnvironmentVariables(this Engine engine, Dictionary<string, string> env)
    {
        engine.SetValue("env", env);
    }

    public static void SetHooks(this Engine engine, Runtime runtime)
    {
        if (runtime.Plugins.Count == 0)
        {
            return;
        }

        foreach (var plugin in runtime.Plugins)
        {
            switch (plugin)
            {
                case Logger.LogHookName:
                    engine.UseConsole();
                    break;
            }
        }
    }
}