namespace Api.Domain.Entities;

public class AppSettings
{
    public bool CheckFunctionExecutionLimitMemory { get; set; }
    public long? FunctionExecutionLimitMemoryBytes { get; set; }
    public bool CheckFunctionExecutionTimeout { get; set; }
    public int? FunctionExecutionTimeoutSeconds { get; set; }
    public bool FunctionUseNoSqlConnection { get; set; }
    
    public static AppSettings Default => new()
    {
        CheckFunctionExecutionLimitMemory = true,
        FunctionExecutionLimitMemoryBytes = 2 * 1024 * 1024,
        CheckFunctionExecutionTimeout = true,
        FunctionExecutionTimeoutSeconds = 10,
        FunctionUseNoSqlConnection = false
    };
}