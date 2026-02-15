namespace Api.Domain.Entities;

public class RouteFolder : BaseEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public App App { get; set; }
    public RouteFolder Parent { get; set; }
    
    public ICollection<RouteFolder> SubFolders { get; set; }
    
    public ICollection<Route> Routes { get; set; }
}