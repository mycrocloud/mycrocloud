namespace WebApp.FunctionShared;

public class Runtime
{
    public Dictionary<string, string> Env { get; set; }

    public HashSet<string> PlugIns { get; set; }
}

public class MycroCloudRuntime
{
    public int AppId { get; set; }
    
    public string ConnectionString { get; set; }
}