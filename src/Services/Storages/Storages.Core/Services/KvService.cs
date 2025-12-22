using Microsoft.EntityFrameworkCore;
using Storages.Core.Data;
using Storages.Core.Entities.Kv;
using Storages.Core.Exceptions;

namespace Storages.Core.Services;

public interface IKvService
{
    Task CreateInstance(string userId, KvInstance instance);
    Task<IReadOnlyList<KvInstance>> ListInstances(string userId);
    Task<KvInstance> GetInstanceById(string userId, Guid instanceId);
    Task DeleteInstance(string userId, Guid instanceId);
}

public class KvService (AppDbContext dbContext) : IKvService
{
    public async Task CreateInstance(string userId, KvInstance instance)
    {
        var existed = await dbContext.KvInstances
            .AnyAsync(i => i.UserId == userId && i.Name == instance.Name);
        
        if (existed)
            throw new BusinessException("Instance with the same name already exists.");
        
        instance.UserId = userId;
        await dbContext.KvInstances.AddAsync(instance);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<KvInstance>> ListInstances(string userId)
    {
        return await dbContext.KvInstances
            .Where(u => u.UserId == userId)
            .ToListAsync();
    }

    public async Task<KvInstance> GetInstanceById(string userId, Guid instanceId)
    {
        var instance = await dbContext.KvInstances
            .SingleOrDefaultAsync(i => i.UserId == userId && i.Id == instanceId);
        
        if (instance is null)
            throw new NotFoundException("Instance not found.");
        
        return instance;
    }

    public async Task DeleteInstance(string userId, Guid instanceId)
    {
        var instance = await dbContext.KvInstances
            .SingleOrDefaultAsync(i => i.UserId == userId && i.Id == instanceId);
        
        if (instance is null)
            throw new NotFoundException("Instance not found.");
        
        dbContext.KvInstances.Remove(instance);
        await dbContext.SaveChangesAsync();
    }
}