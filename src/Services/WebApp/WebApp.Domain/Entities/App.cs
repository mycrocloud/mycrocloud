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

    public RoutingConfig RoutingConfig { get; set; }

    // Navigation properties

    public ICollection<ApiKey> ApiKeys { get; set; }
    public ICollection<AuthenticationScheme> AuthenticationSchemes { get; set; }

    public ICollection<RouteFolder> RouteFolders { get; set; }
    public ICollection<Route> Routes { get; set; }
    public ICollection<Log> Logs { get; set; }
    public ICollection<Variable> Variables { get; set; }

    public AppLink Link { get; set; }

    public AppBuildConfigs BuildConfigs { get; set; }

    public ICollection<AppBuild> AppBuilds { get; set; } = [];
    public AppBuild LatestBuild { get; set; }

    public Guid? LatestBuildId { get; set; }
}

public class AppLink : BaseEntity
{
    public int AppId { get; set; }
    
    //TODO: support other providers e.g. GitLab, Bitbucket
    public long InstallationId { get; set; }
    public long RepoId { get; set; }

    public string RepoName { get; set; }
    
    // Navigation properties
    public GitHubInstallation GitHubInstallation  { get; set; }
}

public class AppBuildConfigs
{
    public string Branch { get; set; }
    public string Directory { get; set; }
    public string OutDir { get; set; }
    public string InstallCommand { get; set; }
    public string BuildCommand { get; set; }
}