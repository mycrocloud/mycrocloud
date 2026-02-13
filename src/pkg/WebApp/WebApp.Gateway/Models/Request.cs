namespace WebApp.Gateway.Models;

public class Request
{
    public required string Method { get; set; }
    public required string Path { get; set; }
    public required Dictionary<string, string> Params { get; set; }
    public Dictionary<string, string>? Query { get; set; }
    public required Dictionary<string, string> Headers { get; set; }
    public string? Body { get; set; }
}