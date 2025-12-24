namespace WebApp.FunctionInvoker.Hooks.Fetch;

public class FetchProxy(int maxCount)
{
    private int _count = 0;
    public async Task<HttpResponseMessage> Fetch(HttpRequestMessage request)
    {
        PreFetch();
        
        var httpClient = new HttpClient(); //TODO: use http client factoring
        return await httpClient.SendAsync(request);
    }

    private void PreFetch()
    {
        _count++;
        if (_count > maxCount)
            throw new CountLimitException();
    }
}