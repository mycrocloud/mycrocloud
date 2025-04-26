using System.Collections.ObjectModel;
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
            ICollection<SecurityKey> keys = new Collection<SecurityKey>();
            foreach (var signingKey in jsonWebKeySet.GetSigningKeys())
                keys.Add(signingKey);
            
            return keys;
        }
        
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{issuer.TrimEnd('/')}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever());
        
        var openIdConnectConfiguration = await configurationManager.GetConfigurationAsync();
        
        await cache.SetAsync(cacheKey,
            System.Text.Encoding.UTF8.GetBytes(openIdConnectConfiguration.JsonWebKeySet.ToString()),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
        
        return openIdConnectConfiguration.SigningKeys;
    }
}