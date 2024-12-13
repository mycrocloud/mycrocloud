using System.ComponentModel.DataAnnotations;

namespace CD.Api;

public class DomainEntity
{
    [Key]
    public required string Domain { get; set; }

    public required string Mcrn { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}