using Api.Domain.Entities;
using Api.Domain.Models;

namespace Api.Infrastructure.Services;

/// <summary>Single C# mapping used by runtime publishing and existing-data backfill.</summary>
public static class AppSpecificationBuilder
{
    public static AppSpecification Build(App app) => new()
    {
        Id = app.Id,
        Slug = app.Slug,
        OwnerId = app.OwnerId,
        State = app.State,
        SpaDeploymentId = app.ActiveSpaDeploymentId,
        ApiDeploymentId = app.ActiveApiDeploymentId,
        Settings = app.Settings ?? AppSettings.Default,
        ApiCorsSettings = app.CorsSettings ?? CorsSettings.Default,
        RoutingConfig = app.RoutingConfig ?? RoutingConfig.Default,
        AuthenticationSchemes = app.AuthenticationSchemes.Where(s => s.Enabled).OrderBy(s => s.Id)
            .Select(s => new CachedAuthenticationScheme
            {
                Type = s.Type,
                OpenIdConnectAuthority = s.OpenIdConnectAuthority,
                OpenIdConnectAudience = s.OpenIdConnectAudience
            }).ToList(),
        Variables = app.Variables.Where(v => v.Target is VariableTarget.Runtime or VariableTarget.All)
            .OrderBy(v => v.Id).Select(v => new CachedVariable { Name = v.Name, Value = v.Value }).ToList()
    };
}
