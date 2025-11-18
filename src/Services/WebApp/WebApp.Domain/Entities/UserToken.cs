using System.ComponentModel.DataAnnotations;

namespace WebApp.Domain.Entities;

public class UserToken
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
    public string Provider { get; set; }
    
    [Required]
    public string Token { get; set; }
    public UserTokenPurpose Purpose { get; set; }
    public TokenStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum UserTokenPurpose
{
    ApiToken
}

public enum TokenStatus
{
    None,
    Revoked
}