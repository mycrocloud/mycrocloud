using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.ApiGateway.Middlewares;

public class StaticResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;

        if (!route.UseDynamicResponse)
        {
            context.Response.StatusCode = route.ResponseStatusCode ??
                                          throw new InvalidOperationException("ResponseStatusCode is null");
        
            foreach (var header in route.ResponseHeaders ?? [])
            {
                context.Response.Headers.Append(header.Name, header.Value);
            }
            
            await context.Response.WriteAsync(route.Response);
            
            return;
        }
        
        var service = new Service();
        
        const string handler = """
                               function handler (request) {
                                 return {
                                     statusCode: responseStatusCode,
                                     headers: responseHeaders,
                                     body: Handlebars.compile(source)({ request })
                                 }
                               }
                               """;
            
        var result = await service.ExecuteJintInDocker(context, app, appRepository, handler, configuration);

        await context.Response.WriteFromFunctionResult(result);
    }
}

public static class StaticResponseMiddlewareExtensions
{
    public static IApplicationBuilder UseStaticResponseMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticResponseMiddleware>();
    }
}