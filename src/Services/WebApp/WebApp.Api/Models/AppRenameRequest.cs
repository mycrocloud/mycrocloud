using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models;

public class AppRenameRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
}
