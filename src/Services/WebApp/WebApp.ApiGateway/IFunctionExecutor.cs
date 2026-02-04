using WebApp.ApiGateway.Models;
using WebApp.Domain.Entities;
using WebApp.Domain.Enums;
using WebApp.Domain.Repositories;

namespace WebApp.ApiGateway;

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
