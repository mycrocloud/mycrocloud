using Api.Domain.Enums;

namespace Api.Domain.Entities;

public class App : BaseEntity
{
    public int Id { get; set; }
    public string OwnerId { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
    public AppState State { get; set; }
    public AppSettings Settings { get; set; }

    public CorsSettings CorsSettings { get; set; }

    public RoutingConfig RoutingConfig { get; set; }

    // Navigation properties

    public ICollection<ApiKey> ApiKeys { get; set; }
    public ICollection<AuthenticationScheme> AuthenticationSchemes { get; set; }

    public ICollection<RouteFolder> RouteFolders { get; set; }
    public ICollection<Route> Routes { get; set; }
    public ICollection<Variable> Variables { get; set; }

    public AppLink Link { get; set; }

    public AppBuildConfigs BuildConfigs { get; set; }

    public ICollection<AppBuild> AppBuilds { get; set; } = [];

    // Active deployments
    public Guid? ActiveSpaDeploymentId { get; set; }
    public SpaDeployment? ActiveSpaDeployment { get; set; }

    public ICollection<Artifact> Artifacts { get; set; } = [];
    public ICollection<SpaDeployment> SpaDeployments { get; set; } = [];
    public ICollection<ApiDeployment> ApiDeployments { get; set; } = [];

    public Guid? ActiveApiDeploymentId { get; set; }
    public ApiDeployment? ActiveApiDeployment { get; set; }
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
    public static readonly AppBuildConfigs Default = new()
    {
        Branch = "main",
        Directory = "",
        OutDir = "dist",
        InstallCommand = "npm install",
        BuildCommand = "npm run build",
        NodeVersion = "20"
    };

    public string Branch { get; set; }
    public string Directory { get; set; }
    public string OutDir { get; set; }
    public string InstallCommand { get; set; }
    public string BuildCommand { get; set; }
    public string NodeVersion { get; set; }
}