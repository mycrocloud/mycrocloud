using System.ComponentModel.DataAnnotations;

namespace Api.Models.Routes;

public class FolderCreateRequest
{
    public int? ParentId { get; set; }
    
    [Required]
    public string Name { get; set; }
}