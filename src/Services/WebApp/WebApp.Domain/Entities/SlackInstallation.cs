
using System.ComponentModel.DataAnnotations;

namespace WebApp.Domain.Entities;

public class SlackInstallation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(32)]
    public string TeamId { get; set; } = default!;

    [MaxLength(128)]
    public string? TeamName { get; set; }

    [MaxLength(32)]
    public string? BotUserId { get; set; }

    [Required]
    [MaxLength(512)]
    public string BotAccessToken { get; set; } = default!;

    [MaxLength(512)]
    public string? Scopes { get; set; }

    [MaxLength(32)]
    public string? InstalledByUserId { get; set; }

    [MaxLength(32)]
    public string? EnterpriseId { get; set; }

    public bool IsEnterpriseInstall { get; set; } = false;

    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SlackUserLink
{
    [Required]
    [MaxLength(32)]
    public string TeamId { get; set; } = null!;

    [Required]
    [MaxLength(32)]
    public string SlackUserId { get; set; } = null!;

    [Required]
    [MaxLength(64)]
    public string UserId { get; set; } = null!;

    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}

public class SlackAppSubscription
{
    [Key]
    public int SubscriptionId { get; set; }
    
    [Required]
    [MaxLength(32)]
    public string TeamId { get; set; } = null!;

    [Required]
    [MaxLength(32)]
    public string SlackUserId { get; set; } = null!;
    
    [Required]
    public int? AppId { get; set; }
}

// public class SlackChannel
// {
//     [Key]
//     [MaxLength(32)]
//     public string ChannelId { get; set; } = default!;

//     [Required]
//     [MaxLength(32)]
//     public string TeamId { get; set; } = default!;

//     [MaxLength(128)]
//     public string? Name { get; set; }

//     public bool Subscribed { get; set; } = false;

//     public DateTime? LastJoinedAt { get; set; }

//     [ForeignKey(nameof(TeamId))]
//     public SlackInstallation? Installation { get; set; }
// }