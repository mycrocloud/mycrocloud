namespace WebApp.Domain.Entities;

public class AppBuildJob
{
    public string Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    public string Status { get; set; }
    public string ContainerId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}