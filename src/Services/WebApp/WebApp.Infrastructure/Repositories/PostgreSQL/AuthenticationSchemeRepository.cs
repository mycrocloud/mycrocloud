using Dapper;
using Npgsql;
using Api.Domain.Repositories;
using Microsoft.Extensions.Options;
using Api.Domain.Entities;

namespace WebApp.Infrastructure.Repositories.PostgreSql;

public class AuthenticationSchemeRepository(IOptions<PostgresDatabaseOptions> databaseOptions) 
    : BaseRepository(databaseOptions), IAuthenticationSchemeRepository
{
    public async Task AddJwtBearerScheme(int appId, JwtBearerAuthenticationScheme scheme)
    {
        const string sql =
"""
insert into 
  jwt_bearer_authentication_scheme (
    app_id, 
    name, 
    display_name, 
    description, 
    issuer, 
    audience, 
  )
values
  (
    @app_id, 
    @name, 
    @display_name, 
    @description, 
    @issuer, 
    @audience
  );
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, new
        {
            app_id = appId,
            name = scheme.Name,
            display_name = scheme.DisplayName,
            description = scheme.Description,
            issuer = scheme.Issuer,
            audience = scheme.Audience
        });
    }

    public async Task<IEnumerable<AuthenticationScheme>> GetAllByAppId(int appId)
    {
        const string sql =
"""
SELECT
    scheme_id SchemeId,
    app_id AppId,
    1 Type,
    name Name,
    display_name DisplayName,
    description Description,
    created_at CreatedAt,
    updated_at UpdatedAt
FROM
    jwt_bearer_authentication_scheme
WHERE
    app_id = @app_id;
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QueryAsync<AuthenticationScheme>(sql, new
        {
            app_id = appId,
        });
    }

    public async Task UpdateJwtBearerScheme(int schemeId, JwtBearerAuthenticationScheme scheme)
    {
        const string sql =
"""
update 
    jwt_bearer_authentication_scheme 
set 
    name = @name,
    display_name = @display_name,
    description = @description,
    issuer = @issuer,
    audience = @audience,
    updated_at = current_timestamp
where 
    scheme_id = @scheme_id;
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, new
        {
            scheme_id = schemeId,
            name = scheme.Name,
            description = scheme.Description,
            issuer = scheme.Issuer,
            audience = scheme.Audience
        });
    }

    public async Task<JwtBearerAuthenticationScheme> GetJwtBearerScheme(int id)
    {
        const string sql =
"""
SELECT
    scheme_id SchemeId,
    app_id AppId,
    name Name,
    display_name DisplayName,
    description Description,
    issuer Issuer,
    audience Audience,
    created_at CreatedAt,
    updated_at UpdatedAt
FROM
    jwt_bearer_authentication_scheme;
WHERE
    scheme_id = @scheme_id
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<JwtBearerAuthenticationScheme>(sql, new
        {
            scheme_id = id
        });
    }
}