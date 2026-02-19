using Api.Domain.Enums;

namespace Api.Domain.Entities;

public class CustomDomain : BaseEntity
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public CustomDomainStatus Status { get; set; } = CustomDomainStatus.Pending;
    public DateTime? VerifiedAt { get; set; }

    public App App { get; set; } = null!;
}
