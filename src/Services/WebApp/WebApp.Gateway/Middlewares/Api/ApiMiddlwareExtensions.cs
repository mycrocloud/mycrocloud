namespace WebApp.Gateway.Middlewares.Api;

public static class ApiMiddlewareExtensions
{
    public static IApplicationBuilder UseApiMiddleware(this IApplicationBuilder builder)
    {
        builder.UseCorsMiddleware();
        builder.UseRouteResolverMiddleware();
        builder.UseAuthenticationMiddleware();
        builder.UseAuthorizationMiddleware();
        builder.UseValidationMiddleware();
        builder.UseResponseDispatcherMiddleware();
        
        return builder;
    }
}