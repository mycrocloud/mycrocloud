using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;

namespace WebApp.Domain.Services;
public interface IAppService {
    Task Create(string userId, App app);
    Task Delete(int id);
    Task Rename(int id, string name);
    Task SetCorsSettings(int id, CorsSettings settings);
    Task SetRoutingConfig(int id, RoutingConfig config);
    Task SetStatus(int id, AppStatus status);
}

public class AppService(IAppRepository appRepository) : IAppService
{
    public async Task Create(string userId, App app)
    {
        await appRepository.Add(userId, app);
    }

    public async Task Delete(int id)
    {
        await appRepository.Delete(id);
    }

    public async Task Rename(int id, string name)
    {
        var currentApp = await appRepository.GetByAppId(id);
        currentApp.Name = name;
        currentApp.Version = Guid.NewGuid();
        await appRepository.Update(id, currentApp);
    }

    public async Task SetCorsSettings(int id, CorsSettings settings)
    {
        var app = await appRepository.GetByAppId(id);
        app.CorsSettings = settings;
        app.Version = Guid.NewGuid();
        await appRepository.Update(id, app);
    }

    public async Task SetRoutingConfig(int id, RoutingConfig config)
    {
        var app = await appRepository.GetByAppId(id);
        app.RoutingConfig = config;
        app.Version = Guid.NewGuid();
        await appRepository.Update(id, app);
    }

    public async Task SetStatus(int id, AppStatus status)
    {
        var app = await appRepository.GetByAppId(id);
        app.Status = status;
        app.Version = Guid.NewGuid();
        await appRepository.Update(id, app);
    }
}
