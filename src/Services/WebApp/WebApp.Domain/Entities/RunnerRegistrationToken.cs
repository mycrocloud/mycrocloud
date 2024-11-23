namespace WebApp.Domain.Entities;

public class RunnerRegistrationToken
{
    public int Id { get; set; }
    public string Token { get; set; }

    public RunnerRegistrationTokenScope Scope { get; set; }

    public string UserId { get; set; }

    public int? AppId { get; set; }
    public App App { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
}

public enum RunnerRegistrationTokenScope
{
    User = 1,
    App
}