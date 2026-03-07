using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Infrastructure.Repositories;

public class RouteRepository(AppDbContext dbContext) : IRouteRepository
{
    public async Task<int> Add(int appId, Route route)
    {
        route.AppId = appId;
        await dbContext.Routes.AddAsync(route);
        await dbContext.SaveChangesAsync();
        return route.Id;
    }

    public async Task Delete(int id)
    {
        var route = await dbContext.Routes.FirstOrDefaultAsync(r => r.Id == id);
        dbContext.Routes.Remove(route);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Route> GetById(int id)
    {
        return await dbContext.Routes
                .FirstAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Route>> List(int appId, string searchTerm, string sort)
    {
        return await dbContext.Routes.Where(r => r.AppId == appId).ToListAsync();
    }

    public async Task Update(int id, Route route)
    {
        dbContext.Routes.Update(route);
        await dbContext.SaveChangesAsync();
    }

    public async Task<Route> GetByIdAsNoTracking(int id)
    {
        return await dbContext.Routes
                .AsNoTracking()
                .FirstAsync(r => r.Id == id);
    }
}
