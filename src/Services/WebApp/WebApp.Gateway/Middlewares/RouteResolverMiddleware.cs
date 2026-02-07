using Microsoft.AspNetCore.Routing.Template;
using WebApp.Domain.Enums;
using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares;

public class RouteResolverMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var app = (CachedApp)context.Items["_CachedApp"]!;
        var matchedRoutes = new List<CachedRoute>();

        foreach (var r in app.Routes)
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
        context.Items["_CachedRoute"] = route;
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