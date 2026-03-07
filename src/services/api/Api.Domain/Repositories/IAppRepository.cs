namespace Api.Domain.Repositories;
using Entities;
public interface IAppRepository
{
    Task<App> GetByAppId(int id);
    Task<App> FindByAppId(int id);
    Task<App> FindByName(string name);
    Task<App> FindByUserIdAndAppName(string userId, string name);
    Task<IEnumerable<App>> ListByUserId(string userId, string query, string sort);
    Task Add(string userId, App app);
    Task Update(int id, App app);
    Task Delete(int appId);
    Task<List<AuthenticationScheme>> GetAuthenticationSchemes(int appId);
    Task<List<Variable>> GetVariables(int appId);
}