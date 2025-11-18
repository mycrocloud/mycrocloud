namespace WebApp.Domain.Entities;

public class GitHubInstallation : BaseEntity
{
    public long InstallationId { get; set; }
    public long AccountId { get; set; }
    public string AccountLogin { get; set; } = string.Empty;
    public GitHubAccountType AccountType { get; set; }

    public string UserId { get; set; }
    
    // Navigation properties
    public ICollection<AppLink> AppLinks { get; set; }
}

public enum GitHubAccountType
{
    User,
    Organization
}