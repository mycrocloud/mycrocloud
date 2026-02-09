using Api.Domain.Models;
using WebApp.Gateway.Models;
using Api.Domain.Enums;

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
