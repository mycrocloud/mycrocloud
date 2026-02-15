namespace WebApp.FunctionInvoker;

public class Request
{
    public string Method { get; set; }
    public string Path { get; set; }
    public Dictionary<string, string> Params { get; set; }
    public Dictionary<string, string> Query { get; set; }
    public Dictionary<string, string> Headers { get; set; }
    public string Body { get; set; }
}