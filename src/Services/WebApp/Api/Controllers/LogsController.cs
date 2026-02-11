using Api.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Domain.Repositories;

namespace Api.Controllers;

[Route("apps/{appId:int}/[controller]")]
[TypeFilter<AppOwnerActionFilter>(Arguments = ["appId"])]
public class LogsController(ILogRepository logRepository) : BaseController
{
    public async Task<IActionResult> Search(int appId, [FromQuery]List<int>? routeIds, DateTime? accessDateFrom,
     DateTime? accessDateTo, int page = 1, int pageSize = 50, string? sort = null) {
        var logs = await logRepository.Search(appId);
        if (routeIds?.Count > 0) logs = logs.Where(l => l.RouteId != null && routeIds.Contains(l.RouteId.Value));
        if (accessDateFrom is not null)
        {
            var fromUtc = DateTime.SpecifyKind(accessDateFrom.Value.Date, DateTimeKind.Utc);
            logs = logs.Where(l => l.CreatedAt >= fromUtc);
        }
        if (accessDateTo is not null)
        {
            var toUtc = DateTime.SpecifyKind(accessDateTo.Value.Date.AddDays(1), DateTimeKind.Utc);
            logs = logs.Where(l => l.CreatedAt < toUtc);
        }
        if (!string.IsNullOrEmpty(sort))
        {
            //TODO: Implement sorting
        }

        var totalItems = await logs.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var data = await logs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .Select(l => new {
                l.Id,
                Timestamp = l.CreatedAt,
                l.RemoteAddress,
                l.RouteId,
                l.Method,
                l.Path,
                l.StatusCode,
                l.FunctionRuntime,
                l.FunctionExecutionDuration,
                l.FunctionLogs,
                l.RequestContentLength,
                l.RequestContentType,
                l.RequestCookie,
                l.RequestFormContent,
                l.RequestHeaders
            })
            .ToListAsync();

        return Ok(new
        {
            Data = data,
            Pagination = new
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats(int appId, DateTime? accessDateFrom, DateTime? accessDateTo)
    {
        var logs = await logRepository.Search(appId);

        if (accessDateFrom is not null)
        {
            var fromUtc = DateTime.SpecifyKind(accessDateFrom.Value.Date, DateTimeKind.Utc);
            logs = logs.Where(l => l.CreatedAt >= fromUtc);
        }
        if (accessDateTo is not null)
        {
            var toUtc = DateTime.SpecifyKind(accessDateTo.Value.Date.AddDays(1), DateTimeKind.Utc);
            logs = logs.Where(l => l.CreatedAt < toUtc);
        }

        var totalRequests = await logs.CountAsync();

        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var tomorrowUtc = todayUtc.AddDays(1);
        var todayRequests = await logs.CountAsync(l => l.CreatedAt >= todayUtc && l.CreatedAt < tomorrowUtc);

        var dailyStats = await logs
            .GroupBy(l => l.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return Ok(new
        {
            TotalRequests = totalRequests,
            TodayRequests = todayRequests,
            DailyStats = dailyStats.Select(d => new
            {
                Date = d.Date.ToString("yyyy-MM-dd"),
                d.Count
            })
        });
    }
}
