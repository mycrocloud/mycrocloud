using System.ComponentModel.DataAnnotations;
using Storages.Core.Entities.Kv;

namespace Storages.Api.Models.Kv;

public class CreateInstanceRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; }

    public KvInstance ToKvInstanceEntity()
    {
        return new KvInstance()
        {
            Name = Name
        };
    }
}