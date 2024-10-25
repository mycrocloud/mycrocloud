using WebApp.Domain.Enums;

namespace WebApp.Domain.Entities;

public class App : BaseEntity
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public AppStatus Status { get; set; }
    public AppSettings Settings { get; set; }
    
    public CorsSettings CorsSettings { get; set; }
    
    // Navigation properties
    
    public ICollection<ApiKey> ApiKeys { get; set; }
    public ICollection<AuthenticationScheme> AuthenticationSchemes { get; set; }
    public ICollection<Folder> Folders { get; set; }
    
    public ICollection<RouteFolder> RouteFolders { get; set; }
    public ICollection<Route> Routes { get; set; }
    public ICollection<Log> Logs { get; set; }
    public ICollection<TextStorage> TextStorages { get; set; }
    public ICollection<Variable> Variables { get; set; }
    
    public ICollection<Object> Objects { get; set; }
    
    //todo: move to separate entity
    public string GitHubRepoFullName { get; set; }
    public string GitHubWebhookToken { get; set; }
}