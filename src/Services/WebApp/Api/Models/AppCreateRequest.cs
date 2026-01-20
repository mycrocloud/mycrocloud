using System.ComponentModel.DataAnnotations;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;

namespace Api.Models;

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
            BuildConfigs = new AppBuildConfigs
            {
                Branch = null,
                Directory = ".",
                OutDir = "dist",
                InstallCommand = "npm install",
                BuildCommand = "npm run build"
            },
            Version = Guid.NewGuid()
        };
    }
}