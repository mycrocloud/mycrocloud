using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Api.Domain.Entities;
using Api.Domain.Enums;
using Route = Api.Domain.Entities.Route;

namespace Api.Models.Routes;

public class RouteCreateUpdateRequest : IValidatableObject
{
    [Required] public string Name { get; set; }
    [Required] public string Method { get; set; }
    [Required] public string Path { get; set; }
    public bool RequireAuthorization { get; set; }
    public string? RequestQuerySchema { get; set; }
    public string? RequestHeaderSchema { get; set; }
    public string? RequestBodySchema { get; set; }

    [Required]
    public RouteResponseRequest Response { get; set; }

    public bool Enabled { get; set; } = true;

    public Route ToCreateEntity()
    {
        return new Route
        {
            Name = Name,
            Method = Method,
            Path = Path,
            RequestQuerySchema = RequestQuerySchema,
            RequestHeaderSchema = RequestHeaderSchema,
            RequestBodySchema = RequestBodySchema,
            ResponseType = Response.Type,
            ResponseStatusCode = Response.Type == ResponseType.Static
                ? Response.StaticResponse?.StatusCode ?? 200
                : null,
            ResponseHeaders = Response.Type == ResponseType.Static
                ? (Response.StaticResponse?.Headers ?? []).Select(h => h.ToEntity()).ToList()
                : [],
            Response = Response.Type == ResponseType.Function
                ? Response.FunctionResponse?.SourceCode
                : Response.StaticResponse?.Content,
            RequireAuthorization = RequireAuthorization,
            Enabled = Enabled,
            FunctionRuntime = Response.Type == ResponseType.Function ? FunctionRuntime.JintInDocker : null
        };
    }

    public void ToUpdateEntity(ref Route route)
    {
        route.Name = Name;
        route.Method = Method;
        route.Path = Path;
        route.RequestQuerySchema = RequestQuerySchema;
        route.RequestHeaderSchema = RequestHeaderSchema;
        route.RequestBodySchema = RequestBodySchema;
        route.ResponseType = Response.Type;
        route.ResponseStatusCode = Response.Type == ResponseType.Static
            ? Response.StaticResponse?.StatusCode ?? 200
            : null;
        route.ResponseHeaders = Response.Type == ResponseType.Static
            ? (Response.StaticResponse?.Headers ?? []).Select(h => h.ToEntity()).ToList()
            : [];
        route.Response = Response.Type == ResponseType.Function
            ? Response.FunctionResponse?.SourceCode
            : Response.StaticResponse?.Content;
        route.RequireAuthorization = RequireAuthorization;
        route.Enabled = Enabled;
        route.FunctionRuntime = Response.Type == ResponseType.Function ? FunctionRuntime.JintInDocker : null;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Response is null)
        {
            yield return new ValidationResult("Response is required.", [nameof(Response)]);
            yield break;
        }

        if (Response.Type != ResponseType.Static && Response.Type != ResponseType.Function)
        {
            yield return new ValidationResult("Response type must be Static or Function.", [$"{nameof(Response)}.{nameof(RouteResponseRequest.Type)}"]);
        }

        if (Response.Type == ResponseType.Static)
        {
            if (Response.StaticResponse is null)
            {
                yield return new ValidationResult("staticResponse is required when response.type is Static.", [$"{nameof(Response)}.{nameof(RouteResponseRequest.StaticResponse)}"]);
            }

            if (Response.FunctionResponse is not null)
            {
                yield return new ValidationResult("functionResponse is not allowed when response.type is Static.", [$"{nameof(Response)}.{nameof(RouteResponseRequest.FunctionResponse)}"]);
            }
        }

        if (Response.Type == ResponseType.Function)
        {
            if (Response.FunctionResponse is null)
            {
                yield return new ValidationResult("functionResponse is required when response.type is Function.", [$"{nameof(Response)}.{nameof(RouteResponseRequest.FunctionResponse)}"]);
            }

            if (string.IsNullOrWhiteSpace(Response.FunctionResponse?.SourceCode))
            {
                yield return new ValidationResult("sourceCode is required when response.type is Function.", [$"{nameof(Response)}.{nameof(RouteResponseRequest.FunctionResponse)}.{nameof(FunctionResponseRequest.SourceCode)}"]);
            }

            if (Response.StaticResponse is not null)
            {
                yield return new ValidationResult("staticResponse is not allowed when response.type is Function.", [$"{nameof(Response)}.{nameof(RouteResponseRequest.StaticResponse)}"]);
            }
        }
    }
}

public class RouteResponseRequest
{
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResponseType Type { get; set; }

    public StaticResponseRequest? StaticResponse { get; set; }
    public FunctionResponseRequest? FunctionResponse { get; set; }
}

public class StaticResponseRequest
{
    public int? StatusCode { get; set; }
    public List<ResponseHeaderRequest> Headers { get; set; } = [];
    public string? Content { get; set; }
}

public class FunctionResponseRequest
{
    public string? SourceCode { get; set; }
}

public class ResponseHeaderRequest
{
    [Required] public string Name { get; set; }
    [Required] public string Value { get; set; }

    public ResponseHeader ToEntity()
    {
        return new ResponseHeader
        {
            Name = Name,
            Value = Value
        };
    }
}
