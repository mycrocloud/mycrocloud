using System.ComponentModel.DataAnnotations;
using Api.Domain.Entities;
using Api.Domain.Enums;

namespace Api.Models;

public class AppCreateRequest
{
    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Name must be lowercase letters, numbers, and hyphens only. Cannot start/end with hyphen or have consecutive hyphens.")]
    public string Name { get; set; }
    
    [MaxLength(400)]
    public string? Description { get; set; }
    
    public App ToEntity()
    {
        return new App
        {
            Slug = Name,
            Description = Description,
            State = AppState.Active,
            CorsSettings = CorsSettings.Default,
            RoutingConfig = RoutingConfig.Default,
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