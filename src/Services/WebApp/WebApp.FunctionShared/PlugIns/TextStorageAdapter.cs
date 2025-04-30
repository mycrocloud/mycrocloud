using WebApp.Infrastructure;

namespace WebApp.FunctionShared.PlugIns;

public class TextStorageAdapter(int appId, string name, AppDbContext appDbContext)
{
    public const string HookName = "useTextStorage";

    public string Read()
    {
        var storage = appDbContext.TextStorages.Single(s => s.AppId == appId && s.Name == name);
        return storage.Content ?? "";
    }

    public void Write(string content)
    {
        var storage = appDbContext.TextStorages.Single(s => s.AppId == appId && s.Name == name);
        storage.Content = content;
        appDbContext.SaveChanges();
    }
}
