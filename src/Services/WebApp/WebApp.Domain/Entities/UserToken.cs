namespace WebApp.Domain.Entities;

public class UserToken
{
    public string UserId { get; set; }
    public string Token { get; set; }
    public string Provider { get; set; }
    public UserTokenPurpose Purpose { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum UserTokenPurpose
{
    AppIntegration,
}