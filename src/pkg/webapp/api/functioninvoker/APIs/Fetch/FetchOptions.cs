namespace WebApp.FunctionInvoker.Apis.Fetch;

public class FetchOptions
{
    public int MaxRequestCount { get; init; } = 50;
    public int MaxRequestBodyBytes { get; init; } = 1 * 1024 * 1024;       // 1 MB
    public int MaxResponseBodyBytes { get; init; } = 5 * 1024 * 1024;      // 5 MB
    public int TimeoutPerRequestSeconds { get; init; } = 5;
    public int MaxRedirects { get; init; } = 5;
    public long MaxTotalBytes { get; init; } = 10 * 1024 * 1024;           // 10 MB
    public int MaxRecursionDepth { get; init; } = 3;
    public int CurrentDepth { get; init; } = 0;

    public const string DepthHeaderName = "X-MycroCloud-Depth";
    public const string DepthEnvVarName = "MYCROCLOUD_FUNCTION_DEPTH";
}
