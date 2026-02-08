using WebApp.Gateway.Cache;

namespace WebApp.Gateway.Middlewares.Api;

/// <summary>
/// Middleware that dispatches requests to the appropriate response handler based on the route's ResponseType.
/// This is a terminal middleware - it does not call next() as handlers generate the final response.
/// </summary>
public class ResponseDispatcherMiddleware(RequestDelegate next, ILogger<ResponseDispatcherMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IEnumerable<IResponseHandler> handlers)
    {
        var route = (CachedRoute)context.Items["_CachedRoute"]!;

        logger.LogDebug("Dispatching response for route {RouteId} with ResponseType {ResponseType}",
            route.Id, route.ResponseType);

        var handler = handlers.FirstOrDefault(h => h.SupportedType == route.ResponseType);

        if (handler is null)
        {
            logger.LogError("No handler found for ResponseType {ResponseType} on route {RouteId}",
                route.ResponseType, route.Id);
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"Response type '{route.ResponseType}' is not supported.");
            return;
        }

        logger.LogDebug("Using handler {HandlerType} for route {RouteId}",
            handler.GetType().Name, route.Id);

        await handler.HandleAsync(context);
    }
}

public static class ResponseDispatcherMiddlewareExtensions
{
    public static IApplicationBuilder UseResponseDispatcherMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ResponseDispatcherMiddleware>();
    }
}
