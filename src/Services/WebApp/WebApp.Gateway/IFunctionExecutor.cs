using WebApp.Gateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;

namespace WebApp.Gateway;

public interface IFunctionExecutor
{
    FunctionRuntime Runtime { get; }

    Task<FunctionResult> ExecuteAsync(
        HttpContext context,
        App app,
        IAppRepository appRepository,
        string handler,
        Dictionary<string, string>? values);
}
