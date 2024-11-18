namespace WebApp.FunctionShared;

public interface IExecutor
{
    Result Execute(Request request, string handler, Dictionary<string, string> env);
}