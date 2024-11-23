using Jint;
using WebApp.FunctionShared;
using WebApp.Infrastructure;

namespace WebApp.MiniApiGateway.Middlewares;

public class StaticResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, Scripts scripts)
    {
        var route = (Route)context.Items["_Route"]!;
        
        context.Response.StatusCode = route.ResponseStatusCode ??
                                      throw new InvalidOperationException("ResponseStatusCode is null");
        
        foreach (var header in route.ResponseHeaders ?? [])
        {
            context.Response.Headers.Append(header.Name, header.Value);
        }

        var body = route.ResponseBody;
        if (route.UseDynamicResponse)
        {
            var engine = new Engine();
            engine.SetRequestValue(await context.Request.Normalize());
            body = engine
                .SetValue("source", body)
                .Execute(scripts.Handlebars)
                .Execute("Handlebars.registerHelper('json', function(context) { return JSON.stringify(context); });")
                .Evaluate("Handlebars.compile(source)({ request });")
                .AsString();
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