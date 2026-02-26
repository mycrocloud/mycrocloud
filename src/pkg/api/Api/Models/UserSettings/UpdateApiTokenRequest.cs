using System.ComponentModel.DataAnnotations;

namespace Api.Models.UserSettings;

public class UpdateApiTokenRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
}