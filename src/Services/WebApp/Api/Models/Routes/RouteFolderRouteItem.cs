using WebApp.Domain.Enums;

namespace Api.Models.Routes;

public enum RouteRouteFolderType
{
    Folder = 1,
    Route = 2
}

public class RouteFolderRouteItem
{
    public RouteRouteFolderType Type { get; set; }
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
    #region Route
    
    public string? RouteName { get; set; }

    public string? RouteMethod { get; set; }
    
    public string? RoutePath { get; set; }
    
    public RouteStatus? RouteStatus { get; set; }
    
    #endregion

    #region Folder
    
    public string? FolderName { get; set; }
    
    #endregion
}