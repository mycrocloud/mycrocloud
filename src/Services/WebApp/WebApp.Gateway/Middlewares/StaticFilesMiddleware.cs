// using WebApp.Infrastructure;
// namespace WebApp.Gateway.Middlewares;
//
// public class StaticFilesMiddleware(RequestDelegate next)
// {
//     public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
//     {
//         var route = (Route)context.Items["_Route"]!;
//         var file = await dbContext.Files.FindAsync(route.FileId);
//         if (file is null)
//         {
//             context.Response.StatusCode = 404;
//             await context.Response.WriteAsync("File not found");
//             await context.Response.CompleteAsync();
//             return;
//         }
//
//         foreach (var header in route.ResponseHeaders ?? [])
//         {
//             context.Response.Headers.Append(header.Name, header.Value);
//         }
//
//         await context.Response.Body.WriteAsync(file.Content);
//         await context.Response.CompleteAsync();
//     }
// }
//
// public static class StaticFilesMiddlewareExtensions
// {
//     public static IApplicationBuilder UseStaticFilesMiddleware(this IApplicationBuilder builder)
//     {
//         return builder.UseMiddleware<StaticFilesMiddleware>();
//     }
// }