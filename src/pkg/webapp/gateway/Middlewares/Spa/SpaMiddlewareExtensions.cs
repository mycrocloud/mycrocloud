namespace MycroCloud.WebApp.Gateway.Middlewares.Spa;

public static class SpaMiddlewareExtensions
{
    public static IApplicationBuilder UseSpaMiddleware(this IApplicationBuilder builder)
    {
        builder.UseSpaStaticFileMiddleware();
        return builder;
    }
}