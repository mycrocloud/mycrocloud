using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Infrastructure.Repositories;

public class LogRepository(AppDbContext appDbContext) : ILogRepository
{
    public async Task Add(Log log)
    {
        await appDbContext.Logs.AddAsync(log);
        await appDbContext.SaveChangesAsync();
    }

    public async Task DeleteByRouteId(int id)
    {
        var logs = appDbContext.Logs.Where(l => l.RouteId == id);
        appDbContext.Logs.RemoveRange(logs);
        await appDbContext.SaveChangesAsync();
    }

    public Task<IQueryable<Log>> Search(int appId)
    {
        return Task.FromResult(appDbContext.Logs
            .Include(l => l.Route)
            .Where(l => l.AppId == appId));
    }
}
