using System.ComponentModel.DataAnnotations;

namespace Storages.Core.Entities.Kv;

public class KvValue
{
    public Guid InstanceId { get; set; }
    public string Key { get; set; }
    
    [Required]
    public string Value { get; set; }
}