namespace Api.Domain.Entities;

public class AppBuildArtifact : BaseEntity
{
    public Guid BuildJobId { get; set; }
    public AppBuild BuildJob { get; set; } = null!;
    public Guid ArtifactId { get; set; }
    public Artifact Artifact { get; set; } = null!;
    public string Role { get; set; } = "primary";
}