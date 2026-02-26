namespace Api.Domain.Entities;

public class CorsSettings
{
    public List<string> AllowedHeaders { get; set; }
    public List<string> AllowedMethods { get; set; }
    public List<string> AllowedOrigins { get; set; }
    public List<string> ExposeHeaders { get; set; }
    public int? MaxAgeSeconds { get; set; }
    public static CorsSettings Default = new() {
        AllowedHeaders = [],
        AllowedMethods = [],
        AllowedOrigins = [],
        ExposeHeaders = [],
        MaxAgeSeconds = null
    };
}