using System.ComponentModel.DataAnnotations;

namespace WebApp.Api.Models.Routes;

public class FolderRenameRequest
{
    [Required]
    public string Name { get; set; }
}