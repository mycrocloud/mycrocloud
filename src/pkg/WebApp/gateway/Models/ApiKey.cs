namespace MycroCloud.WebApp.Gateway.Models;

public class ApiKey
{
    public int Id { get; set; }
    public int AppId { get; set; }

    public required string Name { get; set; }
    
    public required string Key { get; set; }

    public string? Metadata { get; set; }
}