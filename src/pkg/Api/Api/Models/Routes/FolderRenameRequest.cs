using System.ComponentModel.DataAnnotations;

namespace Api.Models.Routes;

public class FolderRenameRequest
{
    [Required]
    public string Name { get; set; }
}