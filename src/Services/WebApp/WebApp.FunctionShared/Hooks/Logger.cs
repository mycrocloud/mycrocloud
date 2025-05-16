using Jint;

namespace WebApp.FunctionShared.Hooks;

public class Logger
{
    public const string LogHookName = "log";

    public const string Code = """
                               const console = {};
                               ['log', 'info', 'warn', 'error'].forEach(level => {
                                 console[level] = function(message) {
                                   log(message);
                                 };
                               });
                               """;
}

public static class LoggerExtension
{
    public static void UseLogger(this Engine engine, Runtime runtime)
    {
        engine.SetValue(Logger.LogHookName, runtime.LogAction);
        engine.Execute(Logger.Code);
    }
}