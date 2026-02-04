using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using Route = WebApp.Domain.Entities.Route;

namespace Api.Models.Routes;

public class RouteCreateUpdateRequest
{
    [Required] public string Name { get; set; }
    [Required] public string Method { get; set; }
    [Required] public string Path { get; set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ResponseType ResponseType { get; set; }

    public int? ResponseStatusCode { get; set; }
    public List<ResponseHeader> ResponseHeaders { get; set; } = [];
    public string? Response { get; set; }
    public string? ResponseBodyLanguage { get; set; }
    public string? FunctionHandler { get; set; }
    public List<string>? FunctionHandlerDependencies { get; set; }
    public bool RequireAuthorization { get; set; }
    public string? RequestQuerySchema { get; set; }
    public string? RequestHeaderSchema { get; set; }
    public string? RequestBodySchema { get; set; }
    public int? FileId { get; set; }

    public int? FolderId { get; set; }

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
            ResponseType = ResponseType,
            ResponseStatusCode = ResponseStatusCode,
            ResponseHeaders = ResponseHeaders.Select(h => h.ToEntity()).ToList(),
            ResponseBodyLanguage = ResponseBodyLanguage,
            Response = Response,
            FunctionHandlerDependencies = FunctionHandlerDependencies,
            RequireAuthorization = RequireAuthorization,
            Enabled = Enabled,
            FunctionRuntime = ResponseType == ResponseType.Function ? FunctionRuntime.JintInDocker : null
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
        route.ResponseType = ResponseType;
        route.ResponseStatusCode = ResponseStatusCode;
        route.ResponseHeaders = ResponseHeaders.Select(h => h.ToEntity()).ToList();
        route.ResponseBodyLanguage = ResponseBodyLanguage;
        route.Response = Response;
        route.FunctionHandlerDependencies = FunctionHandlerDependencies;
        route.RequireAuthorization = RequireAuthorization;
        route.Enabled = Enabled;
    }
}

public class ResponseHeader
{
    [Required] public string Name { get; set; }
    [Required] public string Value { get; set; }

    public WebApp.Domain.Entities.ResponseHeader ToEntity()
    {
        return new WebApp.Domain.Entities.ResponseHeader
        {
            Name = Name,
            Value = Value
        };
    }
}