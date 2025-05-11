namespace WebApp.ApiGateway;

//https://stackoverflow.com/a/69111159/6430433
public static class ConfigurationHelper
{
    public static IConfiguration? Configuration;
    public static void Initialize(IConfiguration configuration)
    {
        Configuration = configuration;
    }
}
