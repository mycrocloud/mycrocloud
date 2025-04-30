using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;

namespace WebApp.Infrastructure.Repositories;

public class AppRepository(AppDbContext dbContext) : IAppRepository
{
    public async Task Add(string userId, App app)
    {
        app.UserId = userId;
        await dbContext.Apps.AddAsync(app);
        //TODO: refactoring
        await dbContext.Folders.AddAsync(new Folder {
            App = app,
            Name = "/",
            ParentId = null,
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(int appId)
    {
        var app = await dbContext.Apps.FirstAsync(a => a.Id == appId);
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
        var apps = dbContext.Apps.Where(a => a.UserId == userId);
        if (!string.IsNullOrEmpty(query))
        {
            apps = apps.Where(a => a.Name.Contains(query) || a.Description.Contains(query));
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

        return variables.ToDictionary(v => v.Name, v => v.StringValue);
    }
}