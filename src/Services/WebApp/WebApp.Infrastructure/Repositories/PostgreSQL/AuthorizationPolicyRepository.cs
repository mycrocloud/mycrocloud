using Dapper;
using Npgsql;
using WebApp.Domain.Repositories;
using Microsoft.Extensions.Options;
using WebApp.Domain.Entities;

namespace WebApp.Infrastructure.Repositories.PostgreSql;

public class AuthorizationPolicyRepository(IOptions<PostgresDatabaseOptions> databaseOptions) : BaseRepository(databaseOptions), IAuthorizationPolicyRepository
{
    public async Task Add(int appId, AuthorizationPolicyEntity policy)
    {
        const string sql =
"""
insert into 
  authorization_policy (
    app_id, 
    name, 
    description
  )
values
  (
    @app_id, 
    @name, 
    @description
  );
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, new
        {
            app_id = appId,
            name = policy.Name,
            description = policy.Description,
        });
    }

    public async Task Delete(int policyId)
    {
        const string sql =
"""
delete from 
  authorization_policy 
where 
  policy_id = @policy_id;
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, new
        {
            policy_id = policyId
        });
    }

    public async Task<AuthorizationPolicyEntity> Get(int policyId)
    {
        const string sql =
"""
SELECT
    policy_id PolicyId,
    app_id AppId,
    name Name,
    description Description,
    updated_at CreatedAt,
    created_at UpdatedAt
FROM
    authorization_policy
WHERE
    policy_id = @policy_id
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<AuthorizationPolicyEntity>(sql, new
        {
            policy_id = policyId
        });
    }

    public async Task<IEnumerable<AuthorizationPolicyEntity>> GetAllByAppId(int appId)
    {
        const string sql =
"""
SELECT
    policy_id PolicyId,
    app_id AppId,
    name Name,
    description Description,
    updated_at CreatedAt,
    created_at UpdatedAt
FROM
    authorization_policy
WHERE
    app_id = @app_id
ORDER BY
    created_at DESC
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QueryAsync<AuthorizationPolicyEntity>(sql, new
        {
            app_id = appId
        });
    }

    public async Task Update(int id, AuthorizationPolicyEntity policy)
    {
        var query =
"""
update 
  authorization_policy 
set 
  name = @name,
  description = @description,
  updated_at = current_timestamp
where 
  policy_id = @policy_id;
""";
        using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(query, new
        {
            policy_id = id,
            name = policy.Name,
            description = policy.Description,
        });
    }
}
