using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using Api.Domain.Entities;
using Api.Domain.Repositories;

namespace WebApp.Infrastructure.Repositories.PostgreSql;

public class RouteRepository(IOptions<PostgresDatabaseOptions> databaseOptions) 
    : BaseRepository(databaseOptions), IRouteRepository
{
    public async Task<int> Add(int appId, Route route)
    {
        const string query = """
insert into 
  route (
    app_id, 
    name, 
    description, 
    match_path, 
    match_order, 
    authorization_type,
    authorization, 
    response_provider
  )
values
  (
    @app_id, 
    @name, 
    @description, 
    @match_path, 
    @match_order, 
    @authorization_type,
    @authorization, 
    @response_provider
  );
RETURNING route_id;
""";

        await using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QuerySingleAsync<int>(query, new
        {
            app_id = appId,
            name = route.Name,
            description = route.Description, 
            // match_path = route.MatchPath,
            // match_order = route.MatchOrder,
            // authorization_type = route.AuthorizationType,
            // authorization = route.Authorization,
            // response_provider = route.ResponseProvider
        });
    }

    public async Task Delete(int id)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var trans = await connection.BeginTransactionAsync();
        try
        {
            const string delete = "DELETE FROM WebApplicationRoute WHERE Id = @Id;";
            await connection.ExecuteAsync(delete, new { Id = id }, trans);
            await trans.CommitAsync();
        }
        catch (Exception)
        {
            await trans.RollbackAsync();
            throw;
        }
    }

    public async Task<Route> Find(int appId, string method, string path)
    {
        const string query = """
SELECT
    route_id,
    web_application_id,
    integration_type,
    name,
    method,
    path,
    description,
    "authorization",
    request_query,
    request_header,
    request_body
FROM
    route
WHERE
    web_application_id = @web_application_id AND
    upper(method) = upper(@method) AND 
    upper(path) = upper(@path);
""";
        await using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<Route>(query, new
        {
            web_application_id = appId,
            method = method,
            path = path
        });
    }

    public async Task<Route> GetById(int id)
    {
        const string query = """
SELECT
    r.route_id RouteId
    ,app_id AppId
    ,name Name
    ,description Description
    ,match_path MatchPath
    ,COALESCE(JSON_AGG(rmmb.method) FILTER (WHERE rmmb.method IS NOT NULL), '[]') MatchMethods
    ,match_order MatchOrder
    ,authorization_type AuthorizationType
    ,"authorization" Authorization
    ,response_provider ResponseProvider
    ,created_at CreatedAt
    ,updated_at UpdatedAt
FROM route r
LEFT JOIN route_match_method_bind rmmb ON rmmb.route_id = r.route_id
WHERE
    r.route_id = @route_id
GROUP BY
    r.route_id;
""";
        await using var connection = new NpgsqlConnection(ConnectionString);
        SqlMapper.AddTypeHandler(new JsonTypeHandler<MatchMethodCollection>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<RouteAuthorization>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<RouteValidation>());
        SqlMapper.AddTypeHandler(new JsonTypeHandler<RouteResponse>());
        return await connection.QuerySingleOrDefaultAsync<Route>(query, new
        {
            route_id = id
        });
    }

    public async Task<IEnumerable<Route>> GetByApplicationId(int appId, string searchTerm, string sort)
    {
        var query =
"""
SELECT 
    route_id RouteId,
    web_application_id WebAppId,
    "name" Name,
    description Description
FROM
    route
WHERE
    web_application_id = @web_application_id
/**/
""";
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query +=
                    """
                    AND "name" LIKE @query OR "path" LIKE @query
                    """;
        }
        if (!string.IsNullOrEmpty(sort))
        {
            query +=
                """
                /**/
                ORDER BY id
                """;
        }

        await using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QueryAsync<Route>(query, new
        {
            web_application_id = appId,
            query = "%" + searchTerm + "%"
        });
    }

    public async Task Update(int id, Route route)
    {
        const string sql = """

""";
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            route.Name,
            route.Description
        });
    }

    public async Task<RouteMockResponse> GetMockResponse(int routeId)
    {
        const string query = """

""";
        await using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<RouteMockResponse>(query, new
        {
            route_id = routeId
        });
    }

    public async Task<IEnumerable<Route>> List(int appId, string searchTerm, string sort)
    {
        var query =
"""
SELECT
    r.route_id RouteId
    ,app_id AppId
    ,name Name
    ,description Description
    ,match_path MatchPath
    ,COALESCE(JSON_AGG(rmmb.method) FILTER (WHERE rmmb.method IS NOT NULL), '[]') MatchMethods
    ,match_order MatchOrder
    ,authorization_type AuthorizationType
    ,"authorization" Authorization
    ,response_provider ResponseProvider
    ,created_at CreatedAt
    ,updated_at UpdatedAt
FROM route r
LEFT JOIN route_match_method_bind rmmb ON rmmb.route_id = r.route_id
WHERE
    app_id = @app_id
GROUP BY
    r.route_id;
""";
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query +=
                    """
                    AND war."name" LIKE @query OR "path" LIKE @query
                    """;
        }
        if (!string.IsNullOrEmpty(sort))
        {
            query +=
                """
                /**/
                ORDER BY route_id
                """;
        }
        SqlMapper.AddTypeHandler(new JsonTypeHandler<MatchMethodCollection>());
        await using var connection = new NpgsqlConnection(ConnectionString);
        return await connection.QueryAsync<Route>(query, new
        {
            web_application_id = appId,
            query = "%" + searchTerm + "%"
        });
    }

    public async Task AddMatchMethods(int id, List<string> methods)
    {
        const string sql = """
delete from 
route_match_method_bind 
where 
route_id = @route_id

insert into 
  route_match_method_bind (
    route_id, 
    method
  )
values
  (
    @route_id, 
    @method
  );
""";
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.ExecuteAsync(sql, methods.Select(m => new { route_id = id, method = m}));
    }
}