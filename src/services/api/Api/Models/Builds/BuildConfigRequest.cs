using System.ComponentModel.DataAnnotations;

namespace Api.Models.Builds;

public class BuildConfigRequest
{
    [Required(ErrorMessage = "Branch is required")]
    [StringLength(255, ErrorMessage = "Branch name cannot exceed 255 characters")]
    public string Branch { get; set; }
    
    [StringLength(500, ErrorMessage = "Directory path cannot exceed 500 characters")]
    public string? Directory { get; set; }
    
    [StringLength(1000, ErrorMessage = "Install command cannot exceed 1000 characters")]
    public string? InstallCommand { get; set; }
    
    [StringLength(1000, ErrorMessage = "Build command cannot exceed 1000 characters")]
    public string? BuildCommand { get; set; }
    
    [StringLength(500, ErrorMessage = "Output directory cannot exceed 500 characters")]
    public string? OutDir { get; set; }
    
    [StringLength(50, ErrorMessage = "Node version cannot exceed 50 characters")]
    public string? NodeVersion { get; set; }
}