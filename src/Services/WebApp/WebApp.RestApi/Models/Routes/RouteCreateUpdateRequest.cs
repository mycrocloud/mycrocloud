using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using WebApp.Domain.Entities;
using Route = WebApp.Domain.Entities.Route;

namespace WebApp.RestApi.Models.Routes;

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
    public string? ResponseBody { get; set; }
    public bool UseDynamicResponse { get; set; }
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
            ResponseBody = ResponseBody,
            UseDynamicResponse = UseDynamicResponse,
            FunctionHandler = FunctionHandler,
            FunctionHandlerDependencies = FunctionHandlerDependencies,
            RequireAuthorization = RequireAuthorization,
            FileId = FileId,
            Enabled = Enabled,
            FunctionExecutionEnvironment =
                ResponseType == ResponseType.Function ? FunctionExecutionEnvironment.InProcess : null
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
        route.ResponseBody = ResponseBody;
        route.UseDynamicResponse = UseDynamicResponse;
        route.FunctionHandler = FunctionHandler;
        route.FunctionHandlerDependencies = FunctionHandlerDependencies;
        route.RequireAuthorization = RequireAuthorization;
        route.FileId = FileId;
        route.Enabled = Enabled;
    }
}

public class ResponseHeader
{
    [Required] public string Name { get; set; }
    [Required] public string Value { get; set; }

    public Domain.Entities.ResponseHeader ToEntity()
    {
        return new Domain.Entities.ResponseHeader
        {
            Name = Name,
            Value = Value
        };
    }
}