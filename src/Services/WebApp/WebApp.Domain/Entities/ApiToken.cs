using System.ComponentModel.DataAnnotations;

namespace WebApp.Domain.Entities;

public class ApiToken
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
    
    [Required]
    public string Token { get; set; }
    public TokenStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum TokenStatus
{
    Active
}