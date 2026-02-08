using WebApp.Domain.Models;
using WebApp.Gateway.Models;
using WebApp.Domain.Enums;

namespace WebApp.Gateway;

public interface IFunctionExecutor
{
    FunctionRuntime Runtime { get; }

    Task<FunctionResult> ExecuteAsync(
        HttpContext context,
        AppSpecification app,
        string handler,
        Dictionary<string, string>? values);
}
