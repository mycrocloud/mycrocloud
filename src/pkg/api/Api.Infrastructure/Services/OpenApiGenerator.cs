using System.Text.Json;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Api.Domain.Models;
using Api.Domain.Services;

namespace Api.Infrastructure.Services;

public class OpenApiGenerator : IOpenApiGenerator
{
    public string GenerateSpecification(string appName, string appSlug, IEnumerable<ApiRouteMetadata> routes)
    {
        var spec = new
        {
            openapi = "3.0.3",
            info = new
            {
                title = appName,
                description = $"API specification for {appName}",
                version = "1.0.0"
            },
            servers = new[]
            {
                new { url = $"https://{appSlug}.mycrocloud.app", description = "Production server" }
            },
            paths = GeneratePaths(routes),
            components = new
            {
                securitySchemes = new
                {
                    bearerAuth = new
                    {
                        type = "http",
                        scheme = "bearer",
                        bearerFormat = "JWT"
                    }
                }
            }
        };

        return JsonSerializer.Serialize(spec, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private Dictionary<string, object> GeneratePaths(IEnumerable<ApiRouteMetadata> routes)
    {
        var paths = new Dictionary<string, object>();

        foreach (var route in routes.OrderBy(r => r.Path))
        {
            var path = route.Path;
            var method = route.Method.ToLower();

            if (!paths.ContainsKey(path))
            {
                paths[path] = new Dictionary<string, object>();
            }

            var pathItem = (Dictionary<string, object>)paths[path];
            pathItem[method] = GenerateOperation(route);
        }

        return paths;
    }

    private object GenerateOperation(ApiRouteMetadata route)
    {
        var operation = new Dictionary<string, object>
        {
            ["summary"] = route.Name ?? string.Empty,
            ["description"] = route.Description ?? string.Empty,
            ["operationId"] = $"{route.Method.ToLower()}_{route.Path.Replace("/", "_").Replace("{", "").Replace("}", "").TrimStart('_')}",
            ["tags"] = new[] { ExtractTagFromPath(route.Path) },
            // Custom extensions for MycroCloud-specific metadata
            ["x-response-type"] = route.ResponseType.ToString(),
            ["x-function-runtime"] = route.Response.FunctionResponse?.Runtime?.ToString()
        };

        // Add parameters (query, path, header)
        var parameters = new List<object>();

        // Add path parameters
        var pathParams = ExtractPathParameters(route.Path);
        foreach (var param in pathParams)
        {
            parameters.Add(new
            {
                name = param,
                @in = "path",
                required = true,
                schema = new { type = "string" }
            });
        }

        // Add query parameters from schema
        if (!string.IsNullOrEmpty(route.RequestQuerySchema))
        {
            try
            {
                var querySchema = JsonSerializer.Deserialize<JsonElement>(route.RequestQuerySchema);
                if (querySchema.ValueKind == JsonValueKind.Object && querySchema.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                    {
                        parameters.Add(new
                        {
                            name = prop.Name,
                            @in = "query",
                            required = false,
                            schema = prop.Value
                        });
                    }
                }
            }
            catch { /* Ignore invalid JSON */ }
        }

        // Add header parameters from schema
        if (!string.IsNullOrEmpty(route.RequestHeaderSchema))
        {
            try
            {
                var headerSchema = JsonSerializer.Deserialize<JsonElement>(route.RequestHeaderSchema);
                if (headerSchema.ValueKind == JsonValueKind.Object && headerSchema.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                    {
                        parameters.Add(new
                        {
                            name = prop.Name,
                            @in = "header",
                            required = false,
                            schema = prop.Value
                        });
                    }
                }
            }
            catch { /* Ignore invalid JSON */ }
        }

        if (parameters.Any())
        {
            operation["parameters"] = parameters;
        }

        // Add request body
        if (!string.IsNullOrEmpty(route.RequestBodySchema))
        {
            try
            {
                var bodySchema = JsonSerializer.Deserialize<object>(route.RequestBodySchema);
                operation["requestBody"] = new
                {
                    required = true,
                    content = new
                    {
                        application_json = new { schema = bodySchema }
                    }
                };
            }
            catch { /* Ignore invalid JSON */ }
        }

        // Add responses
        var statusCode = route.ResponseType == ResponseType.Static
            ? (route.Response.StaticResponse?.StatusCode?.ToString() ?? "200")
            : "200";

        object responseContent = route.ResponseType switch
        {
            ResponseType.Static => new Dictionary<string, object>
            {
                ["application/json"] = new { schema = new { type = "object" } }
            },
            ResponseType.Function => new Dictionary<string, object>
            {
                ["application/json"] = new { schema = new { type = "object" } }
            },
            _ => new Dictionary<string, object>()
        };
        
        operation["responses"] = new Dictionary<string, object>
        {
            [statusCode] = new
            {
                description = GetResponseDescription(route.ResponseType, statusCode),
                content = responseContent
            }
        };

        // Add security requirement
        if (route.RequireAuthorization)
        {
            operation["security"] = new[] { new { bearerAuth = Array.Empty<string>() } };
        }

        return operation;
    }

    private string ExtractTagFromPath(string path)
    {
        var parts = path.TrimStart('/').Split('/');
        return parts.Length > 0 ? parts[0] : "default";
    }

    private List<string> ExtractPathParameters(string path)
    {
        var parameters = new List<string>();
        var segments = path.Split('/');

        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                parameters.Add(segment.Trim('{', '}'));
            }
        }

        return parameters;
    }

    private string GetResponseDescription(ResponseType responseType, string statusCode)
    {
        if (responseType == ResponseType.Function)
        {
            return "Function execution response";
        }

        return statusCode switch
        {
            "200" => "Successful response",
            "201" => "Resource created",
            "204" => "No content",
            "400" => "Bad request",
            "401" => "Unauthorized",
            "403" => "Forbidden",
            "404" => "Not found",
            "500" => "Internal server error",
            _ => "Static content response"
        };
    }
}
