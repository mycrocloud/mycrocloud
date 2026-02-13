using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.Gateway.Services;

public interface ICachedOpenIdConnectionSigningKeys
{
    Task<ICollection<SecurityKey>> Get(string issuer);
}

public class CachedOpenIdConnectionSigningKeys(IDistributedCache cache, IHttpClientFactory httpClientFactory) : ICachedOpenIdConnectionSigningKeys
{
    public async Task<ICollection<SecurityKey>> Get(string issuer)
    {
        var cacheKey = $"OpenIdConnectionSigningKeys:{issuer}:Keys";
        
        var keys = await cache.GetStringAsync(cacheKey);
        
        if (keys is null)
        {
            var address = $"{issuer}/.well-known/openid-configuration";
            var httpClient = httpClientFactory.CreateClient("HttpDocumentRetriever");
            var retriever = new HttpDocumentRetriever(httpClient);
            var doc = await retriever.GetDocumentAsync(address, default);
            var openIdConnectConfiguration = OpenIdConnectConfiguration.Create(doc);
            keys = await retriever.GetDocumentAsync(openIdConnectConfiguration.JwksUri, default);
                
            await cache.SetAsync(cacheKey,
                System.Text.Encoding.UTF8.GetBytes(keys),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });
        }

        return new JsonWebKeySet(keys).GetSigningKeys();
    }
}