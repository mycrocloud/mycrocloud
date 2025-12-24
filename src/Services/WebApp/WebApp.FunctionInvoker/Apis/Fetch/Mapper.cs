using Jint;
using Jint.Native;

namespace WebApp.FunctionInvoker.Apis.Fetch;

public static class Mapper
{
    public static HttpRequestMessage MapRequest(JsValue input, JsValue? init = null)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get
        };

        if (input.IsString())
        {
            request.RequestUri = new Uri(input.AsString());
        }

        if (init is not null)
        {
            var method = init.Get("method").AsString();
            request.Method = HttpMethod.Parse(method);
        }

        return request;
    }

    public static JsValue MapResponse(HttpResponseMessage response)
    {
        var content = response.Content.ReadAsStringAsync().Result;
        return new JsString(content);
    }
}