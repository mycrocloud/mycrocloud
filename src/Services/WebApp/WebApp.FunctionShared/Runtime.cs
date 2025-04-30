using WebApp.FunctionShared.Hooks;

namespace WebApp.FunctionShared;

public class Runtime
{
    public Dictionary<string, string> Env { get; set; }

    public HashSet<string> Hooks { get; set; } = [TextStorage.HookName, ObjectStorage.HookName];
}

public class MycroCloudRuntime
{
    public int AppId { get; set; }
    
    public string ConnectionString { get; set; }
}