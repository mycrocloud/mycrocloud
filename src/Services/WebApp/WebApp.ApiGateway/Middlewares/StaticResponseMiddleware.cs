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
                                     statusCode: 200, //mc_responseStatusCode,
                                     headers: {}, //mc_responseHeaders,
                                     body: Handlebars.compile(mc_responseBody)({ request })
                                 }
                               }
                               """;

        var stringValues = new Dictionary<string, string>
        {
            { "mc_responseBody", route.Response }
        };

        var numberValues = new Dictionary<string, long>
        {
            { "mc_responseStatusCode", route.ResponseStatusCode ?? 500 }
        };

        var dictValues = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "mc_responseHeaders", (route.ResponseHeaders ?? []).ToDictionary(k => k.Name, v => v.Value)
            }
        };
            
        var result = await service.ExecuteJintInDocker(context, app, appRepository, handler, configuration, stringValues);

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