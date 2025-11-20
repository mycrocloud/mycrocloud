namespace WebApp.ApiGateway.Models;

public class Result
{
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public TimeSpan Duration { get; set; }
    public string Log { get; set; }
}