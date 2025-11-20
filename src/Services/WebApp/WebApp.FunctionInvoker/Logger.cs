using Jint;

namespace WebApp.FunctionInvoker;

public class Logger
{
    public const string LogHookName = "log";
}

public static class LoggerExtension
{
    private const string Code = """
                                const console = {};
                                ['log', 'info', 'warn', 'error'].forEach(level => {
                                  console[level] = function(message) {
                                    log(message);
                                  };
                                });
                                """;
    public static void UseConsole(this Engine engine)
    {
        engine.SetValue(Logger.LogHookName, new Action<object>(Console.WriteLine));
        engine.Execute(Code);
    }
}