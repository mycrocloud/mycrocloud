using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Infrastructure.Repositories;

public class LogRepository(AppDbContext appDbContext) : ILogRepository
{
    public async Task Add(AccessLog accessLog)
    {
        await appDbContext.Logs.AddAsync(accessLog);
        await appDbContext.SaveChangesAsync();
    }

    public async Task DeleteByRouteId(int id)
    {
        var logs = appDbContext.Logs.Where(l => l.RouteId == id);
        appDbContext.Logs.RemoveRange(logs);
        await appDbContext.SaveChangesAsync();
    }

    public Task<IQueryable<AccessLog>> Search(int appId)
    {
        return Task.FromResult(appDbContext.Logs
            .Where(l => l.AppId == appId));
    }
}
