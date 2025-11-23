namespace WebApp.FunctionInvoker;

public class Result
{
    public int? StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = [];
    public string? Body { get; set; }
    public TimeSpan Duration { get; set; }

    public string? ExceptionType { get; set; }
    public string? Exception { get; set; }
}