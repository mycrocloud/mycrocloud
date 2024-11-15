namespace WebApp.FunctionShared;

public class Result
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
    public string AdditionalLogMessage { get; set; }
}