using System.Text.Json.Serialization;
using WebApp.FunctionShared.Hooks;

namespace WebApp.FunctionShared;

public class Runtime
{
    public Dictionary<string, string> Env { get; set; }

    public HashSet<string> Hooks { get; set; } = [TextStorage.HookName, ObjectStorage.HookName];


    public long? MemoryLimit { get; set; }
    
    [JsonIgnore]
    public int AppId { get; set; }
    
    [JsonIgnore]
    public string ConnectionString { get; set; }
}