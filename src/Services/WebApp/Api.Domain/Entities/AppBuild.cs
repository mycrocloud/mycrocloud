namespace Api.Domain.Entities;

public class AppBuild : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    public string Status { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    // Metadata stored as JSONB in PostgreSQL
    public Dictionary<string, string> Metadata { get; set; } = new();
    
    // Navigation - A build can create the initial deployment,
    // but the same artifact can be deployed multiple times
    public ICollection<SpaDeployment> Deployments { get; set; } = [];
}

public static class BuildMetadataKeys
{
    public const string CommitSha = "commitSha";
    public const string CommitMessage = "commitMessage";
    public const string Branch = "branch";
    public const string Author = "author";
}

public class AppBuildState
{
    public const string queued = "queued";
    public const string running = "running";
    public const string succeeded = "succeeded";
    public const string failed = "failed";
}