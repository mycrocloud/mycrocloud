using Jint;
using Npgsql;
using WebApp.FunctionShared.Hooks;

namespace WebApp.FunctionShared;

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
    
    public static void SetHooks(this Engine engine, HashSet<string> hooks, int appId, string connectionString)
    {
        if (hooks.Count == 0)
        {
            return;
        } 
        
        var connection = new NpgsqlConnection(connectionString);
        
        foreach (var plugin in hooks)
        {
            switch (plugin)
            {
                case TextStorage.HookName:
                    engine.SetValue(TextStorage.HookName,
                        new Func<string, object>(name =>
                        {
                            var adapter = new TextStorage(appId, name, connection);

                            return new
                            {
                                read = new Func<string>(() => adapter.Read()),
                                write = new Action<string>(content => adapter.Write(content))
                            };
                        }));
                    break;
                
                case ObjectStorage.HookName:
                    engine.SetValue(ObjectStorage.HookName,
                        () =>
                        {
                            var adapter = new ObjectStorage(appId, connection);

                            return new
                            {
                                read = new Func<string, byte[]>(key => adapter.Read(key)),
                                write = new Action<string, byte[]>((key, content) => adapter.Write(key, content))
                            };
                        });
                    break;
            }
        }
    }
}