namespace Api.Domain.Entities;

public class ApiKey : BaseEntity
{
    public int Id { get; set; }

    public App App { get; set; }
    public string Name { get; set; }
    
    public string Key { get; set; }

    public string? Metadata { get; set; }
}