using System.Reflection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.MiniApiGateway;

public interface ICachedOpenIdConnectionSigningKeys
{
    Task<ICollection<SecurityKey>> Get(string issuer);
}

public class CachedOpenIdConnectionSigningKeys(IDistributedCache cache) : ICachedOpenIdConnectionSigningKeys
{
    public async Task<ICollection<SecurityKey>> Get(string issuer)
    {
        var cacheKey = $"OpenIdConnectionSigningKeys:{issuer}";
        
        var cachedJsonWebKeySet = await cache.GetStringAsync(cacheKey);
        
        if (cachedJsonWebKeySet is not null)
        {
            var jsonWebKeySet = new JsonWebKeySet(cachedJsonWebKeySet);
            
            return jsonWebKeySet.GetSigningKeys();
        }
        
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{issuer.TrimEnd('/')}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
        
        var openIdConnectConfiguration = await configurationManager.GetConfigurationAsync();
        
        var tokensAssembly = typeof(SecurityKey).Assembly;
        var serializerType = tokensAssembly.GetType("Microsoft.IdentityModel.Tokens.JsonWebKeySetSerializer", throwOnError: true)!;
        var writeMethod = serializerType.GetMethod(
            "Write",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            new[] { typeof(JsonWebKeySet) },
            null
        )!;
        
        var jsonWebKeySetJson = (string)writeMethod.Invoke(null, [openIdConnectConfiguration.JsonWebKeySet])!;
        
        await cache.SetAsync(cacheKey,
            System.Text.Encoding.UTF8.GetBytes(jsonWebKeySetJson),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
            });
        
        return openIdConnectConfiguration.SigningKeys;
    }
}