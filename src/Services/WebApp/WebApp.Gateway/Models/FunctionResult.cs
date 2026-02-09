using Api.Domain.Entities;

namespace WebApp.Gateway.Models;

public class FunctionResult
{
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public TimeSpan Duration { get; set; }
    public ICollection<FunctionLogEntry> Logs { get; set; }
}