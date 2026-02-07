namespace WebApp.Domain.Entities;

public class Release : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public Guid? SpaDeploymentId { get; set; }
    public SpaDeployment? SpaDeployment { get; set; }
}
