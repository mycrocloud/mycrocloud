using Microsoft.EntityFrameworkCore;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace Api.Infrastructure.Repositories;

public class AppRepository(AppDbContext dbContext) : IAppRepository
{
    public async Task Add(string userId, App app)
    {
        app.OwnerId = userId;
        await dbContext.Apps.AddAsync(app);
        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(int appId)
    {
        var app = await dbContext.Apps.FirstAsync(a => a.Id == appId);

        // Break references from App -> Deployment first, then remove deployments.
        // This avoids FK conflicts during app deletion when artifacts are still linked.
        app.ActiveSpaDeploymentId = null;
        app.ActiveApiDeploymentId = null;
        await dbContext.SaveChangesAsync();

        await dbContext.Set<AppLink>()
            .Where(link => link.AppId == appId)
            .ExecuteDeleteAsync();

        await dbContext.SpaDeployments
            .Where(d => d.AppId == appId)
            .ExecuteDeleteAsync();

        await dbContext.ApiDeployments
            .Where(d => d.AppId == appId)
            .ExecuteDeleteAsync();

        dbContext.Apps.Remove(app);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AuthenticationScheme>> GetAuthenticationSchemes(int appId)
    {
        return await dbContext.AuthenticationSchemes
            .Where(a => a.AppId == appId)
            .ToListAsync();
    }

    public async Task<App> FindByAppId(int id)
    {
        return await dbContext.Apps.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<App> FindByName(string name)
    {
        return await dbContext.Apps.FirstOrDefaultAsync(a => a.Slug == name);
    }

    public Task<App> FindByUserIdAndAppName(string userId, string name)
    {
        throw new NotImplementedException();
    }

    public async Task<App> GetByAppId(int id)
    {
        return await dbContext.Apps.FirstAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<App>> ListByUserId(string userId, string query, string sort)
    {
        var apps = dbContext.Apps.Where(a => a.OwnerId == userId);
        if (!string.IsNullOrEmpty(query))
        {
            apps = apps.Where(a => a.Slug.Contains(query) || a.Description.Contains(query));
        }
        if (!string.IsNullOrEmpty(sort))
        {
            //TODO: Implement sorting
        }
        return await apps.ToListAsync();
    }

    public async Task Update(int id, App app)
    {
        dbContext.Apps.Update(app);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<Variable>> GetVariables(int appId)
    {
        return await dbContext.Variables.Where(v => v.AppId == appId).ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetEnvironmentVariables(int appId)
    {
        var variables = await GetVariables(appId);

        return variables.ToDictionary(v => v.Name, v => v.Value);
    }
}
