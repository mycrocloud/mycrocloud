using Microsoft.EntityFrameworkCore;
using Storages.Core.Data;
using Storages.Core.Entities.Kv;
using Storages.Core.Exceptions;

namespace Storages.Core.Services;

public interface IKvValueService
{
    Task<KvValue> ReadValue(string userId, Guid instanceId, string key);
    Task WriteValue(string userId, Guid instanceId, string key, string value);
}

public class KvValueService (AppDbContext dbContext) : IKvValueService
{
    public async Task<KvValue> ReadValue(string userId, Guid instanceId, string key)
    {
        var instance = await dbContext.KvInstances
            .SingleOrDefaultAsync(i => i.UserId == userId && i.Id == instanceId);
        
        if (instance is null)
            throw new NotFoundException("Instance not found.");
        
        var kvValue = await dbContext.KvValues
            .SingleOrDefaultAsync(kv => kv.InstanceId == instanceId && kv.Key == key);
        
        if (kvValue is null)
            throw new NotFoundException("Key not found.");
        
        return kvValue;
    }

    public async Task WriteValue(string userId, Guid instanceId, string key, string value)
    {
        var instance = await dbContext.KvInstances
            .SingleOrDefaultAsync(i => i.UserId == userId && i.Id == instanceId);
        
        if (instance is null)
            throw new NotFoundException("Instance not found.");
        
        var kvValue = await dbContext.KvValues
            .SingleOrDefaultAsync(kv => kv.InstanceId == instanceId && kv.Key == key);
        
        if (kvValue is null)
        {
            kvValue = new KvValue
            {
                InstanceId = instanceId,
                Key = key,
                Value = value
            };
            await dbContext.KvValues.AddAsync(kvValue);
        }
        else
        {
            kvValue.Value = value;
            dbContext.KvValues.Update(kvValue);
        }
        
        await dbContext.SaveChangesAsync();
    }
}