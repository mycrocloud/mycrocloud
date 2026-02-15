namespace Api.Domain.Entities;
public class JwtBearerAuthenticationScheme : AuthenticationScheme
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
}