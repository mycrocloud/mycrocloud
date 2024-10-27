using Microsoft.AspNetCore.Routing.Template;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.MiniApiGateway.Middlewares;

public class RouteResolverMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogRepository logRepository,
        AppDbContext appDbContext)
    {
        var app = (App)context.Items["_App"]!;
        var routes = await appDbContext.Routes.Where(r => r.App == app && r.Enabled).ToListAsync();
        var matchedRoutes = new List<Route>();
        foreach (var r in routes)
        {
            var matcher = new TemplateMatcher(TemplateParser.Parse(r.Path), []);
            if (matcher.TryMatch(context.Request.Path, context.Request.RouteValues) &&
                (r.Method.Equals("ANY") || context.Request.Method.Equals(r.Method, StringComparison.OrdinalIgnoreCase)))
            {
                matchedRoutes.Add(r);
            }
        }

        switch (matchedRoutes.Count)
        {
            case 0:
                context.Response.StatusCode = 404;
                return;
            case > 1:
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("The request matched multiple endpoints");
                return;
        }

        var route = matchedRoutes.First();
        switch (route.Status)
        {
            case RouteStatus.Inactive:
                context.Response.StatusCode = 404;
                return;
            case RouteStatus.Blocked:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("The request matched a blocked endpoint");
                return;
        }

        context.Items["_Route"] = route;
        await next(context);
    }
}

public static class RouteResolverMiddlewareExtensions
{
    public static IApplicationBuilder UseRouteResolverMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RouteResolverMiddleware>();
    }
}