namespace WebApp.Domain.Entities;

public class Object : BaseEntity
{
    public App App { get; set; }
    public int AppId { get; set; }
    public string Key { get; set; }
    public byte[] Content { get; set; }
    public ObjectType Type { get; set; }
}

public enum ObjectType
{
    Default,
    BuildArtifact,
}