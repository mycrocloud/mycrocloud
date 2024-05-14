﻿using WebApp.Domain.Repositories;

namespace WebApp.Domain.Services;

public interface IRouteService
{
    Task Delete(int id);
}
public class RouteService(IRouteRepository routeRepository, ILogRepository logRepository) : IRouteService
{
    public async Task Delete(int id)
    {
        await logRepository.DeleteByRouteId(id);
        await routeRepository.Delete(id);
    }
}
