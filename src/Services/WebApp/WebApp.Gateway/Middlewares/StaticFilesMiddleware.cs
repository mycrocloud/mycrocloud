using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using WebApp.Domain.Entities;
using WebApp.Domain.Repositories;
using WebApp.Infrastructure;

namespace WebApp.Gateway.Middlewares;

public class StaticFilesMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ILogRepository logRepository,
        AppDbContext appDbContext)
    {
        var app = (App)context.Items["_App"]!;

        if (app.LatestBuildId is not null && HttpMethods.IsGet(context.Request.Method))
        {
            var path = context.Request.Path.Value;
            
            //todo: read from app settings
            if (string.IsNullOrEmpty(path) || path == "/")
            {
                path = "/index.html";
            }
            
            path = path.TrimStart('/'); // Note: Temp fix

            var file = await appDbContext.AppBuildArtifacts.SingleOrDefaultAsync(f =>
                f.BuildId == app.LatestBuildId &&
                f.Path == path);
            
            if (file is not null)
            {
                var provider = new FileExtensionContentTypeProvider();
                if (!provider.TryGetContentType(file.Path, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                context.Response.ContentType = contentType;
                await context.Response.Body.WriteAsync(file.Content);
                await context.Response.CompleteAsync();
                return;
            }
        }
        
        await next(context);
    }
}

public static class StaticFilesMiddleware2Extensions
{
    public static IApplicationBuilder UseStaticFilesMiddleware2(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<StaticFilesMiddleware>();
    }
}