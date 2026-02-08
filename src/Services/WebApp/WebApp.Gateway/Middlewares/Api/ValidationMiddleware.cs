using System.Text.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using WebApp.Domain.Models;

namespace WebApp.Gateway.Middlewares.Api;

public class ValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var route = (CachedRoute)context.Items["_CachedRoute"]!;
        if (string.IsNullOrEmpty(route.RequestQuerySchema)
            && string.IsNullOrEmpty(route.RequestHeaderSchema)
            && string.IsNullOrEmpty(route.RequestBodySchema))
        {
            await next(context);
            return;
        }

        List<ValidationError> errors = [];
        bool isValidContentType = true;
        if (!string.IsNullOrEmpty(route.RequestQuerySchema))
        {
            var schema = JSchema.Parse(route.RequestQuerySchema);
            var query = JObject.FromObject(context.Request.Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString()));
            query.IsValid(schema, out IList<ValidationError>? validationErrors);
            errors.AddRange(validationErrors);
        }

        if (!string.IsNullOrEmpty(route.RequestHeaderSchema))
        {
            var schema = JSchema.Parse(route.RequestHeaderSchema);
            var query = JObject.FromObject(context.Request.Headers.ToDictionary());
            query.IsValid(schema, out IList<ValidationError>? validationErrors);
            errors.AddRange(validationErrors);
        }

        if (!string.IsNullOrEmpty(route.RequestBodySchema))
        {
            //TODO: Support other content-type
            if (context.Request.HasJsonContentType())
            {
                var schema = JSchema.Parse(route.RequestBodySchema);
                context.Request.EnableBuffering();
                var bodyString = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;
                var body = JObject.Parse(bodyString);
                body.IsValid(schema, out IList<ValidationError>? validationErrors);
                errors.AddRange(validationErrors);
            }
            else
            {
                isValidContentType = false;
            }
        }

        if (errors.Count == 0 && isValidContentType)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = 400;
        var message = !isValidContentType ? "Body must be in json"
        : JsonSerializer.Serialize(errors.Select(e => e.Message));
        await context.Response.WriteAsync(message);
    }
}

public static class ValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseValidationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidationMiddleware>();
    }
}