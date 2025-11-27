using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Api.Filters;
using WebApp.Domain.Repositories;

namespace WebApp.Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class LogsController(ILogRepository logRepository) : BaseController
{
    public async Task<IActionResult> Search(int appId, [FromQuery]List<int>? routeIds, DateTime? accessDateFrom,
     DateTime? accessDateTo, int page = 1, int pageSize = 50, string? sort = null) {
        var logs = await logRepository.Search(appId);
        if (routeIds?.Count > 0) logs = logs.Where(l => l.RouteId != null && routeIds.Contains(l.RouteId.Value));
        if (accessDateFrom is not null) logs = logs.Where(l => l.CreatedAt.Date >= accessDateFrom.Value.Date);
        if (accessDateTo is not null) logs = logs.Where(l => l.CreatedAt.Date <= accessDateTo.Value.Date);
        if (!string.IsNullOrEmpty(sort))
        {
            //TODO: Implement sorting
        }
        logs = logs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking();
        // add page info to the header
        
        Response.Headers.Append("X-Total-Count", logs.Count().ToString());
        Response.Headers.Append("X-Page", page.ToString());
        Response.Headers.Append("X-Page-Size", pageSize.ToString());
        Response.Headers.Append("X-Page-Count", Math.Ceiling((double)logs.Count() / pageSize).ToString(CultureInfo.InvariantCulture));
        
        return Ok(logs.Select(l => new {
            l.Id,
            Timestamp = l.CreatedAt,
            l.RemoteAddress,
            l.RouteId,
            RouteName = l.Route != null ? l.Route.Name : null,
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
        }));
    }
}
