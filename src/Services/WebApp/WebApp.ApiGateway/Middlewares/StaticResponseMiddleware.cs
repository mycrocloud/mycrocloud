using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.ApiGateway.Middlewares;

public class StaticResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts, IAppRepository appRepository, IConfiguration configuration)
    {
        var app = (App)context.Items["_App"]!;
        var route = (Route)context.Items["_Route"]!;
        
        context.Response.StatusCode = route.ResponseStatusCode ??
                                      throw new InvalidOperationException("ResponseStatusCode is null");
        
        foreach (var header in route.ResponseHeaders ?? [])
        {
            context.Response.Headers.Append(header.Name, header.Value);
        }

        var body = route.Response;
        if (route.UseDynamicResponse)
        {
            var service = new Service();
            
            var handler = $$"""
                            function (request) {
                                return {
                                    statusCode: {{route.ResponseStatusCode}},
                                    headers: {},
                                    body: Handlebars.compile(source)({ request });
                                }
                            }
                            """;
            
            var result = await service.ExecuteJintInDocker(context, app, appRepository, handler, configuration);

            body = result.Body;
        }

        await context.Response.WriteAsync(body);
    }
}

public static class StaticResponseMiddlewareExtensions
{
    public static IApplicationBuilder UseStaticResponseMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticResponseMiddleware>();
    }
}