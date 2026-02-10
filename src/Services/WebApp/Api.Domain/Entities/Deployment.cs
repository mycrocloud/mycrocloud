namespace Api.Domain.Entities;

public abstract class Deployment : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public string? Name { get; set; }
    public DeploymentStatus Status { get; set; }
    
    // Navigation
    public ICollection<DeploymentFile> Files { get; set; } = [];
}
