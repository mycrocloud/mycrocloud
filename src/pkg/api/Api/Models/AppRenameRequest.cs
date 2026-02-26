using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public class AppRenameRequest
{
    [Required]
    [MaxLength(50)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "Name must be lowercase letters, numbers, and hyphens only. Cannot start/end with hyphen or have consecutive hyphens.")]
    public string Name { get; set; }
}
