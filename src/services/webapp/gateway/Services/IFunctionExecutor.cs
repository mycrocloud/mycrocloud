using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Services;

public interface IFunctionExecutor
{
    FunctionRuntime Runtime { get; }

    Task<FunctionResult> ExecuteAsync(
        HttpContext context,
        AppSpecification app,
        string handler,
        Dictionary<string, string>? values);
}
