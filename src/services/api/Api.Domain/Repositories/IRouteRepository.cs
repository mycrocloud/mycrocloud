using Api.Domain.Entities;

namespace Api.Domain.Repositories;

public interface IRouteRepository
{
    Task<IEnumerable<Route>> List(int appId, string searchTerm, string sort);
    Task<int> Add(int appId, Route route);
    Task<Route> GetById(int id);
    Task Update(int id, Route route);
    Task Delete(int id);
    Task<Route> GetByIdAsNoTracking(int id);
}