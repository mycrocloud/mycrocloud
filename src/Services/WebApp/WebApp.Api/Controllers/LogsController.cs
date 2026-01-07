using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Api.Shared;
using WebApp.Domain.Repositories;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class LogsController(ILogRepository logRepository) : BaseController
{
    public async Task<IActionResult> Search(int appId, [FromQuery]List<int>? routeIds, DateTime? accessDateFrom,
     DateTime? accessDateTo, [FromQuery]PaginationParameter pagination) {
        
        var logs = await logRepository.Search(appId);
        var totalCount = await logs.CountAsync();
        if (routeIds?.Count > 0) logs = logs.Where(l => l.RouteId != null && routeIds.Contains(l.RouteId.Value));
        if (accessDateFrom is not null) logs = logs.Where(l => l.CreatedAt.Date >= accessDateFrom.Value.Date);
        if (accessDateTo is not null) logs = logs.Where(l => l.CreatedAt.Date <= accessDateTo.Value.Date);
        
        logs = logs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((pagination.Page - 1) * pagination.PerPage)
            .Take(pagination.PerPage)
            .AsNoTracking();
        
        var items = await logs.ToListAsync();
        
        return Ok(new
        {
            data = items.Select(l => new {
                l.Id,
                Timestamp = l.CreatedAt,
                l.RemoteAddress,
                l.RouteId,
                RouteName = l.Route?.Name,
                l.Method,
                l.Path,
                l.StatusCode,
                l.FunctionExecutionEnvironment,
                l.FunctionExecutionDuration,
                l.FunctionLogs,
                l.RequestContentLength,
                l.RequestContentType,
                l.RequestCookie,
                l.RequestFormContent,
                l.RequestHeaders
            }),
            meta = new
            {
                items.Count,
                pagination.Page,
                per_page = pagination.PerPage,
                total_count = totalCount
            }
        });
    }
}
