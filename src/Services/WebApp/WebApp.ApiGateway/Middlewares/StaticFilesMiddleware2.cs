using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.ApiGateway.Middlewares;

public class StaticFilesMiddleware2(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogRepository logRepository,
        AppDbContext appDbContext)
    {
        var app = (App)context.Items["_App"]!;

        if (HttpMethods.IsGet(context.Request.Method))
        {
            var path = context.Request.Path.Value;
            //todo: read from app settings
            if (path == "/")
            {
                path = "/index.html";
            }

            // var obj = await appDbContext.Objects.SingleOrDefaultAsync(obj => obj.Type == ObjectType.BuildArtifact &&
            //                                                                  obj.AppId == app.Id &&
            //                                                                  obj.Key == path);
            // if (obj is not null)
            // {
            //     var provider = new FileExtensionContentTypeProvider();
            //     if (!provider.TryGetContentType(obj.Key, out var contentType))
            //     {
            //         contentType = "application/octet-stream";
            //     }

            //     context.Response.ContentType = contentType;
            //     await context.Response.Body.WriteAsync(obj.Content);
            //     await context.Response.CompleteAsync();
            //     return;
            // }
        }
        
        await next(context);
    }
}

public static class StaticFilesMiddleware2Extensions
{
    public static IApplicationBuilder UseStaticFilesMiddleware2(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticFilesMiddleware2>();
    }
}