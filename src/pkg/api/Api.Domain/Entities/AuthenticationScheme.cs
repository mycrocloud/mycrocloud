using System.Text.Json.Serialization;
using Api.Domain.Enums;

namespace Api.Domain.Entities;
public class AuthenticationScheme : BaseEntity
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public App App { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AuthenticationSchemeType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string OpenIdConnectAuthority { get; set; }
    public string OpenIdConnectAudience { get; set; }
    public bool Enabled { get; set; }
    public int? Order { get; set; }
}