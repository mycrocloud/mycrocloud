using Jint;
using Microsoft.EntityFrameworkCore;
using WebApp.FunctionShared.PlugIns;
using WebApp.Infrastructure;

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
    
    public static void SetPlugIns(this Engine engine, HashSet<string> plugins, int appId, string connectionString)
    {
        if (plugins.Count == 0)
        {
            return;
        } 
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        var dbContext = new AppDbContext(optionsBuilder.Options);
        
        foreach (var plugin in plugins)
        {
            switch (plugin)
            {
                case TextStorageAdapter.HookName:
                    engine.SetValue(TextStorageAdapter.HookName,
                        new Func<string, object>(name =>
                        {
                            var adapter = new TextStorageAdapter(appId, name, dbContext);

                            return new
                            {
                                read = new Func<string>(() => adapter.Read()),
                                write = new Action<string>(content => adapter.Write(content))
                            };
                        }));
                    break;
                
                case ObjectStorageAdapter.HookName:
                    engine.SetValue(ObjectStorageAdapter.HookName,
                        () =>
                        {
                            var adapter = new ObjectStorageAdapter(appId, dbContext);

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