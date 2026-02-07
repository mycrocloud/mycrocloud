namespace WebApp.Domain.Entities;

public enum DeploymentStatus
{
    Pending = 1,
    Extracting = 2,
    Ready = 3,
    Failed = 4,
    Archived = 5
}

public class SpaDeployment : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public Guid? BuildId { get; set; }
    public AppBuild? Build { get; set; }
    public Guid ArtifactId { get; set; }
    public Artifact Artifact { get; set; } = null!;
    public DeploymentStatus Status { get; set; }
}
