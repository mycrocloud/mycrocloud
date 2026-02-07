using WebApp.Gateway.Cache;
using WebApp.Gateway.Models;
using WebApp.Domain.Enums;

namespace WebApp.Gateway;

public interface IFunctionExecutor
{
    FunctionRuntime Runtime { get; }

    Task<FunctionResult> ExecuteAsync(
        HttpContext context,
        CachedApp app,
        string handler,
        Dictionary<string, string>? values);
}
