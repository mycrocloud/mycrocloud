using System.ComponentModel.DataAnnotations;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;

namespace WebApp.Api.Models;

public class AppCreateRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
    
    [MaxLength(400)]
    public string? Description { get; set; }
    
    public App ToEntity()
    {
        return new App
        {
            Name = Name,
            Description = Description,
            Status = AppStatus.Active,
            CorsSettings = CorsSettings.Default,
            Settings = AppSettings.Default,
            Version = Guid.NewGuid()
        };
    }
}