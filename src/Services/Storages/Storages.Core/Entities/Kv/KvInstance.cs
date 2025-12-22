using System.ComponentModel.DataAnnotations;

namespace Storages.Core.Entities.Kv;

public class KvInstance
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }
}