namespace WebApp.Domain.Entities;

public enum ArtifactType
{
    SpaBundle = 1
}

public class Artifact : BaseEntity
{
    public Guid Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; } = null!;
    public ArtifactType ArtifactType { get; set; }
    public byte[] BlobData { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Compression { get; set; } = "zip";
}
