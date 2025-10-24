using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models.UserSettings;

public class CreateApiTokenRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
}