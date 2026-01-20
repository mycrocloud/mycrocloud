namespace Api.Models.Files;

public class FolderContent
{
    public string Type { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? Size { get; set; }
    public int ParentId { get; set; }
}

public class FolderPathItem
{
    public int Depth { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
}