using Microsoft.Extensions.Options;
using MycroCloud.WebApp.Gateway.Configuration;
using MycroCloud.WebApp.Gateway.Models;

namespace MycroCloud.WebApp.Gateway.Services;

// Resolves the function-execution plan (resource limits) for an app.
//
// Plans are an account-level entitlement. Per-account plans are not persisted yet,
// so every app currently resolves to the configured default plan. When account plans
// land, this is the single place to look them up by app.OwnerId.
public class FunctionPlanResolver(IOptions<FunctionExecutionOptions> options)
{
    private readonly FunctionExecutionOptions _options = options.Value;

    public ResolvedFunctionPlan Resolve(AppSpecification app)
    {
        // TODO: map app.OwnerId -> account plan once account plans are persisted.
        var name = _options.DefaultPlan;
        return new ResolvedFunctionPlan(name, _options.Plans[name]);
    }
}
