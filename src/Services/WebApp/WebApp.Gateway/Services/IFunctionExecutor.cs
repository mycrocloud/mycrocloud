using WebApp.Gateway.Models;

namespace WebApp.Gateway.Services;

public interface IFunctionExecutor
{
    FunctionRuntime Runtime { get; }

    Task<FunctionResult> ExecuteAsync(
        HttpContext context,
        AppSpecification app,
        string handler,
        Dictionary<string, string>? values);
}
