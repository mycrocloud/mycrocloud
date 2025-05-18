using WebApp.Domain.Enums;

namespace WebApp.Domain.Entities;

public class Route : BaseEntity
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    public string Name { get; set; }
    public string Method { get; set; }
    public string Path { get; set; }
    public string Description { get; set; }
    public ResponseType ResponseType { get; set; }
    public int? ResponseStatusCode { get; set; }
    public IList<ResponseHeader> ResponseHeaders { get; set; }
    public string Response { get; set; }
    public string ResponseBodyLanguage { get; set; }
    public string FunctionHandlerMethod { get; set; }
    public IList<string> FunctionHandlerDependencies { get; set; }
    public string RequestQuerySchema { get; set; }
    public string RequestHeaderSchema { get; set; }
    public string RequestBodySchema { get; set; }
    public bool RequireAuthorization { get; set; }
    public RouteStatus Status { get; set; } = RouteStatus.Active;
    public bool UseDynamicResponse { get; set; }
    public int? FileId { get; set; }
    public File File { get; set; }

    public int? FolderId { get; set; }
    public RouteFolder Folder { get; set; }

    public bool Enabled { get; set; }

    public FunctionExecutionEnvironment? FunctionExecutionEnvironment { get; set; }
    
    // Navigation properties
    public ICollection<Log> Logs { get; set; }
}

public class ResponseHeader
{
    public string Name { get; set; }
    public string Value { get; set; }
}

public enum ResponseType
{
    Static = 1,
    StaticFile = 2,
    Function = 3
}

public enum FunctionExecutionEnvironment
{
    InProcess = 1,
    OutOfProcess_DockerContainer = 2,
    OutOfProcess_KVM = 3
}