namespace WebApp.FunctionInvoker;

public class Runtime
{
    public Dictionary<string, string> Env { get; set; }

    public HashSet<string> Plugins { get; set; } = [Logger.LogHookName];

    public long? MemoryLimit { get; set; }
}