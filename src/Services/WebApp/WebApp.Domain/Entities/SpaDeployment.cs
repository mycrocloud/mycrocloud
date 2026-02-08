namespace WebApp.Domain.Entities;

public enum DeploymentStatus
{
    Pending = 1,
    Extracting = 2,
    Ready = 3,
    Failed = 4,
    Archived = 5
}

public class SpaDeployment : Deployment
{
    public Guid? BuildId { get; set; }
    public AppBuild? Build { get; set; }
    public Guid ArtifactId { get; set; }
    public Artifact Artifact { get; set; } = null!;
}

