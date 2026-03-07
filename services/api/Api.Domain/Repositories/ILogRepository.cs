using Api.Domain.Entities;

namespace Api.Domain.Repositories;

public interface ILogRepository
{
    Task Add(AccessLog accessLog);
    Task DeleteByRouteId(int id);
    Task<IQueryable<AccessLog>> Search(int appId);
}
