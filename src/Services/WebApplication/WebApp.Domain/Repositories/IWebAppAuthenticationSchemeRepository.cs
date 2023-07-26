using WebApp.Domain.Entities;
using WebApp.Domain.WebApplication.Shared;

namespace WebApp.Domain.Repositories;

public interface IWebAppAuthenticationSchemeRepository
{
    Task Add(int appId, AuthenticationSchemeEntity scheme);
    Task Update(int appId, AuthenticationSchemeEntity scheme);
    Task Activate(int appId, List<int> schemeIds);
    Task<IEnumerable<AuthenticationSchemeEntity>> GetAll(int appId);
    Task<AuthenticationSchemeEntity> Get(int schemeId);
    Task<AuthenticationSchemeEntity> Get(int schemeId, AuthenticationSchemeType type);
    Task<AuthenticationSchemeEntity> Get<TOptions>(int schemeId) where TOptions : AuthenticationSchemeOptions;
}