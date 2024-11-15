using System.Diagnostics;
using System.Globalization;
using Jint;
using Jint.Native;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.FunctionShared;
using WebApp.FunctionShared.PlugIns;
using WebApp.Infrastructure;

namespace WebApp.MiniApiGateway.Middlewares;

public class FunctionInvokerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts,
        IAppRepository appRepository, InProcessFunctionExecutionManager inProcessFunctionExecutionManager)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;

        FunctionExecutionResult result;
        switch (route.FunctionExecutionEnvironment)
        {
            case FunctionExecutionEnvironment.InProcess:
                result = await InProcess(context, scripts, appRepository, inProcessFunctionExecutionManager, app,
                    route);
                break;
            default:
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Function execution environment not supported.");
                return;
        }

        //Write response
        context.Response.StatusCode = result.StatusCode ?? 200;
        foreach (var (key, value) in result.Headers)
        {
            context.Response.Headers.Append(key, value);
        }

        await context.Response.WriteAsync(result.Body ?? "");
        context.Items["_FunctionExecutionResult"] = result;
    }

    private async Task<FunctionExecutionResult> InProcess(HttpContext context, Scripts scripts,
        IAppRepository appRepository,
        InProcessFunctionExecutionManager inProcessFunctionExecutionManager, App app, Route route)
    {
        var engine = new Engine(options =>
        {
            if (app.Settings.CheckFunctionExecutionLimitMemory)
            {
                var memoryLimit = app.Settings.FunctionExecutionLimitMemoryBytes ?? 1 * 1024 * 1024;
                memoryLimit += 10 * 1024 * 1024;

                options.LimitMemory(memoryLimit);
            }
        });

        //Inject global variables
        await InjectEnvironmentVariables(appRepository, app, engine);

        // Inject utility scripts
        InjectBuiltInUtilityScripts(scripts, engine);

        //Inject user-defined dependencies
        await InjectUserDefinedDependencies(route, engine);

        //Inject plugins
        using var scope = context.RequestServices.CreateScope();
        var dbContext2 = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        InjectPlugIns(engine, dbContext2, app);
        
        JsValue jsResult;
        var result = new FunctionExecutionResult();
        //Start measuring time for function execution
        var startingTimestamp = Stopwatch.GetTimestamp();
        try
        {
            await engine.SetRequestValue(context.Request);
            const string code = "(() => { return $FunctionHandler$(request); })();";

            jsResult = await inProcessFunctionExecutionManager.EnqueueJob(token =>
            {
                engine.Execute(route.FunctionHandler);
                var value = engine.Evaluate(code.Replace("$FunctionHandler$",
                    route.FunctionHandlerMethod ?? "handler"));

                return Task.FromResult(value);
            }, TimeSpan.FromSeconds(app.Settings.FunctionExecutionTimeoutSeconds ?? 10));

            var resultx = JintExecutor.Map(jsResult);
        }
        catch (Exception e)
        {
            jsResult = JsValue.FromObject(engine, new
            {
                statusCode = 500,
                body = e.Message,
                additionalLogMessage = e.Message
            });
            result.Exception = e;
        }
        finally
        {
            result.Duration = Stopwatch.GetElapsedTime(startingTimestamp);
        }

        var additionalLogMessage = jsResult.Get("additionalLogMessage");
        if (additionalLogMessage.IsString())
        {
            result.AdditionalLogMessage = additionalLogMessage.AsString();
        }

        return result;
    }

    private static void InjectPlugIns(Engine engine, AppDbContext dbContext, App app)
    {
        engine.SetValue("useTextStorage",
            new Func<string, TextStorageAdapter>(name => new TextStorageAdapter(app, name, dbContext)));

        engine.SetValue("useObjectStorage",
            () => new ObjectStorageAdapter(app.Id, dbContext));
    }

    private async Task InjectUserDefinedDependencies(Route route, Engine engine)
    {
        foreach (var dependency in route.FunctionHandlerDependencies ?? [])
        {
            var script = await LoadScript(dependency);
            if (!string.IsNullOrEmpty(script))
            {
                engine.Execute(script);
            }
        }
    }

    private static void InjectBuiltInUtilityScripts(Scripts scripts, Engine engine)
    {
        //engine.Execute(scripts.Faker);
        engine.Execute(scripts.Handlebars);
        engine.Execute(scripts.Lodash);
    }

    private static async Task InjectEnvironmentVariables(IAppRepository appRepository, App app, Engine engine)
    {
        var variables = new Dictionary<string, object?>();
        var appVariables = await appRepository.GetVariables(app.Id);
        foreach (var variable in appVariables)
        {
            object? value = variable.ValueType switch
            {
                VariableValueType.String => variable.StringValue,
                VariableValueType.Number => int.Parse(variable.StringValue),
                VariableValueType.Boolean => bool.Parse(variable.StringValue),
                VariableValueType.Null => null,
                _ => throw new ArgumentOutOfRangeException()
            };
            variables[variable.Name] = value;
        }

        engine.SetValue("env", variables);
    }

    private async Task<string> LoadScript(string dependency)
    {
        return string.Empty;
    }
}

public static class FunctionInvokerMiddlewareExtensions
{
    public static IApplicationBuilder UseFunctionInvokerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FunctionInvokerMiddleware>();
    }
}

public class FunctionExecutionResult
{
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public string? AdditionalLogMessage { get; set; }
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }
}