namespace MockServer.Api.TinyFramework;

public interface IMiddleware
{
    Task<MiddlewareInvokeResult> InvokeAsync(Request request);
}
