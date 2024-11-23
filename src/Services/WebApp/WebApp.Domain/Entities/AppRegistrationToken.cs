namespace WebApp.Domain.Entities;

public class AppRegistrationToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}