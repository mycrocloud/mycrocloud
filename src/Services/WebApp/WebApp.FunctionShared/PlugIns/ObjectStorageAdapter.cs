using WebApp.Infrastructure;
using Object = WebApp.Domain.Entities.Object;

namespace WebApp.FunctionShared.PlugIns;

public class ObjectStorageAdapter(int appId, AppDbContext dbContext)
{
    public byte[] Read(string key)
    {
        var storage = dbContext.Objects.Single(o => o.App.Id == appId && o.Key == key);
        return storage.Content;
    }

    public void Write(string key, byte[] content)
    {
        var obj = dbContext.Objects.SingleOrDefault(o => o.App.Id == appId && o.Key == key);
        if (obj is null)
        {
            dbContext.Objects.Add(new Object
            {
                AppId = appId,
                Key = key,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                Version = Guid.NewGuid()
            });
        }
        else
        {
            obj.Content = content;
            obj.UpdatedAt = DateTime.UtcNow;
            obj.Version = Guid.NewGuid();
        }

        dbContext.SaveChanges();
    }
}