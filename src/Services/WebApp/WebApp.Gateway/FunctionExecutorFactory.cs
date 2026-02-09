using Api.Domain.Enums;

namespace WebApp.Gateway;

public class FunctionExecutorFactory(IEnumerable<IFunctionExecutor> executors)
{
    private readonly Dictionary<FunctionRuntime, IFunctionExecutor> _executors =
        executors.ToDictionary(e => e.Runtime);

    public IFunctionExecutor? GetExecutor(FunctionRuntime? runtime)
    {
        if (runtime is null)
            return null;

        return _executors.GetValueOrDefault(runtime.Value);
    }
}
