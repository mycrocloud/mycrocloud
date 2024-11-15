using WebApp.Domain.Entities;
using WebApp.Infrastructure;

namespace WebApp.FunctionShared.PlugIns;

public class TextStorageAdapter(App app, string name, AppDbContext appDbContext)
{
    public string Read()
    {
        var storage = appDbContext.TextStorages.Single(s => s.App == app && s.Name == name);
        return storage.Content ?? "";
    }

    public void Write(string content)
    {
        var storage = appDbContext.TextStorages.Single(s => s.App == app && s.Name == name);
        storage.Content = content;
        appDbContext.SaveChanges();
    }
}
