using System.Text.RegularExpressions;
using Api.Domain.Entities;
using Api.Domain.Models;
using Api.Infrastructure;

namespace WebApp.Gateway.Middlewares;

public class RoutingMiddleware(RequestDelegate next, ILogger<RoutingMiddleware> logger)
{
    public async Task Invoke(HttpContext context, AppDbContext appDbContext)
    {
        var app = (AppSpecification)context.Items["_AppSpecification"]!;
        var isCustomConfig = app.RoutingConfig is { Routes.Count: > 0 };
        var config = isCustomConfig ? app.RoutingConfig : RoutingConfig.Default;

        var requestPath = context.Request.Path.Value ?? "/";

        logger.LogDebug("Routing request: Path={Path}, App={AppName}, UsingCustomConfig={IsCustom}, RouteCount={RouteCount}",
            requestPath, app.Slug, isCustomConfig, config!.Routes.Count);

        // Find matching route by priority (lower priority number = higher precedence)
        var matchedRoute = config.Routes
            .OrderBy(r => r.Priority ?? int.MaxValue)
            .FirstOrDefault(r => MatchRoute(requestPath, r.Match));

        if (matchedRoute is null)
        {
            logger.LogDebug("No route matched for path: {Path}", requestPath);
            context.Response.StatusCode = 404;
            return;
        }

        logger.LogDebug("Route matched: Name={RouteName}, MatchType={MatchType}, MatchPath={MatchPath}, TargetType={TargetType}",
            matchedRoute.Name ?? "(unnamed)", matchedRoute.Match.Type, matchedRoute.Match.Path, matchedRoute.Target.Type);

        // Store the matched routing config route for downstream middlewares
        context.Items["_RoutingConfigRoute"] = matchedRoute;

        switch (matchedRoute.Target.Type)
        {
            case RouteTargetType.Api:
                // Apply strip prefix if configured
                if (matchedRoute.Target.StripPrefix == true)
                {
                    var strippedPath = StripPrefix(requestPath, matchedRoute.Match.Path);
                    logger.LogDebug("Stripped prefix: {OriginalPath} -> {StrippedPath}", requestPath, strippedPath);
                    context.Request.Path = strippedPath;
                }
                // Continue to RouteResolverMiddleware
                await next(context);
                break;

            case RouteTargetType.Static:
                // Continue to StaticFileMiddleware
                await next(context);
                break;

            default:
                logger.LogWarning("Unknown target type: {TargetType}", matchedRoute.Target.Type);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Unknown target type");
                break;
        }
    }

    private static bool MatchRoute(string requestPath, RouteMatch match)
    {
        return match.Type switch
        {
            RouteMatchType.Prefix => requestPath.StartsWith(match.Path, StringComparison.OrdinalIgnoreCase),
            RouteMatchType.Exact => requestPath.Equals(match.Path, StringComparison.OrdinalIgnoreCase),
            RouteMatchType.Regex => Regex.IsMatch(requestPath, match.Path, RegexOptions.IgnoreCase),
            _ => false
        };
    }

    private static string StripPrefix(string requestPath, string prefix)
    {
        if (requestPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var stripped = requestPath[prefix.Length..];
            return string.IsNullOrEmpty(stripped) ? "/" : stripped;
        }
        return requestPath;
    }


}

public static class RoutingMiddlewareExtensions
{
    public static IApplicationBuilder UseRoutingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoutingMiddleware>();
    }
}
