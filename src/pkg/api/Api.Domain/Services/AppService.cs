using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Repositories;

namespace Api.Domain.Services;
public interface IAppService {
    Task Create(string userId, App app);
    Task Delete(int id);
    Task Rename(int id, string slug);
    Task SetCorsSettings(int id, CorsSettings settings);
    Task SetRoutingConfig(int id, RoutingConfig config);
    Task SetState(int id, AppState state);
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

    public async Task Rename(int id, string slug)
    {
        var currentApp = await appRepository.GetByAppId(id);
        currentApp.Slug = slug;
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

    public async Task SetState(int id, AppState state)
    {
        var app = await appRepository.GetByAppId(id);
        app.State = state;
        app.Version = Guid.NewGuid();
        await appRepository.Update(id, app);
    }
}
