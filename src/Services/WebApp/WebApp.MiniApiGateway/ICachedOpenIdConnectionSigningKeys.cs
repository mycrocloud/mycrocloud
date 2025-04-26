using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace WebApp.MiniApiGateway;

public interface ICachedOpenIdConnectionSigningKeys
{
    Task<ICollection<SecurityKey>> Get(string issuer);
}

public class CachedOpenIdConnectionSigningKeys(IDistributedCache cache, IHttpClientFactory httpClientFactory) : ICachedOpenIdConnectionSigningKeys
{
    public async Task<ICollection<SecurityKey>> Get(string issuer)
    {
        var cacheKey = $"OpenIdConnectionSigningKeys:{issuer}";
        
        var doc = await cache.GetStringAsync(cacheKey);
        
        if (doc is null)
        {
            var address = $"{issuer.TrimEnd('/')}/.well-known/openid-configuration";
            var httpClient = httpClientFactory.CreateClient("HttpDocumentRetriever");
            var retriever = new HttpDocumentRetriever(httpClient);
            doc = await retriever.GetDocumentAsync(address, default);
            
            await cache.SetAsync(cacheKey,
                System.Text.Encoding.UTF8.GetBytes(doc),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2)
                });
        }

        var openIdConnectConfiguration = OpenIdConnectConfiguration.Create(doc);
        
        return openIdConnectConfiguration.SigningKeys;
    }
}