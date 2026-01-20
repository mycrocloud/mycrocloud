using System.ComponentModel.DataAnnotations;

namespace Api.Models.UserSettings;

public class CreateApiTokenRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
}