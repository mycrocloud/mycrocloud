namespace WebApp.Domain.Entities;

public class AppBuildArtifact : BaseEntity
{
    public AppBuild Build { get; set; }
    public Guid BuildId { get; set; }
    public string Path { get; set; }
    public byte[] Content { get; set; }
}