using System.Text.Json;
using Api.Domain.Entities;
using Api.Infrastructure.Repositories;
using Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MycroCloud.WebApp.Gateway.Services;
using Npgsql;
using ApiDbContext = Api.Infrastructure.AppDbContext;

namespace Api.MigrationTest;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")))
            Skip = "Set TEST_POSTGRES_CONNECTION_STRING to run isolated PostgreSQL integration tests.";
    }
}

public class AppSpecificationTests
{
    [Fact]
    public void CSharp_builder_produces_gateway_contract_and_filters_runtime_data()
    {
        var app = new App
        {
            Id = 1, Slug = "contract", OwnerId = "owner", State = Api.Domain.Enums.AppState.Disabled,
            ActiveApiDeploymentId = Guid.NewGuid(), ActiveSpaDeploymentId = Guid.NewGuid(),
            AuthenticationSchemes = [
                new AuthenticationScheme { Enabled = true, Type = Api.Domain.Enums.AuthenticationSchemeType.OpenIdConnect },
                new AuthenticationScheme { Enabled = false, Type = Api.Domain.Enums.AuthenticationSchemeType.Basic }
            ],
            Variables = [
                new Variable { Id = 1, Name = "runtime", Value = "one", Target = VariableTarget.Runtime },
                new Variable { Id = 2, Name = "build", Value = "two", Target = VariableTarget.Build },
                new Variable { Id = 3, Name = "both", Value = "three", Target = VariableTarget.All }
            ]
        };
        var json = JsonSerializer.Serialize(AppSpecificationBuilder.Build(app));
        var spec = JsonSerializer.Deserialize<MycroCloud.WebApp.Gateway.Models.AppSpecification>(json)!;
        Assert.Equal("contract", spec.Slug);
        Assert.Equal("owner", spec.OwnerId);
        Assert.Equal(MycroCloud.WebApp.Gateway.Models.AppState.Disabled, spec.State);
        Assert.Equal(app.ActiveApiDeploymentId, spec.ApiDeploymentId);
        Assert.Equal(app.ActiveSpaDeploymentId, spec.SpaDeploymentId);
        Assert.Single(spec.AuthenticationSchemes);
        Assert.Equal(new[] { "runtime", "both" }, spec.Variables.Select(v => v.Name));
        Assert.Equal(AppSettings.Default.FunctionExecutionLimitMemoryBytes, spec.Settings.FunctionExecutionLimitMemoryBytes);
        Assert.Equal(RoutingConfig.Default.Routes.Count, spec.RoutingConfig.Routes.Count);
        Assert.Empty(spec.ApiCorsSettings.AllowedOrigins);
    }

    [PostgreSqlFact]
    public async Task CSharp_backfill_recovers_apps_without_cache_and_preserves_runtime_configuration()
    {
        await using var database = await TestDatabase.CreateAsync(migrate: false);
        await using var db = database.CreateContext();
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync("20260620095155_SetNullOnAccessLogRouteDelete");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "Apps" ("Id", "Slug", "OwnerId", "State", "CreatedAt", "Version")
            VALUES (1, 'existing', 'owner', 2, now(), gen_random_uuid());
            INSERT INTO "AuthenticationSchemes" ("AppId", "Type", "Enabled", "CreatedAt", "Version")
            VALUES (1, 1, true, now(), gen_random_uuid()), (1, 3, false, now(), gen_random_uuid());
            INSERT INTO "Variables" ("AppId", "Name", "Value", "Target", "IsSecret", "CreatedAt", "Version")
            VALUES (1, 'RUNTIME', 'runtime', 0, false, now(), gen_random_uuid()),
                   (1, 'BUILD', 'build', 1, false, now(), gen_random_uuid()),
                   (1, 'ALL', 'all', 2, true, now(), gen_random_uuid());
            DELETE FROM cache_entries;
            """);
        await migrator.MigrateAsync();
        Assert.Null(await database.Store.GetBySlugAsync("existing"));
        Assert.Equal(1, await new AppSpecificationStore(db).BackfillAsync());
        var noFunction = await db.Database.SqlQueryRaw<bool>(
            "SELECT to_regprocedure('refresh_app_specification(integer)') IS NULL AS \"Value\"").SingleAsync();
        Assert.True(noFunction);
        var spec = await database.Store.GetBySlugAsync("existing");
        Assert.NotNull(spec);
        Assert.Equal(MycroCloud.WebApp.Gateway.Models.AppState.Disabled, spec.State);
        Assert.Equal("owner", spec.OwnerId);
        Assert.Single(spec.AuthenticationSchemes);
        Assert.Equal(new[] { "RUNTIME", "ALL" }, spec.Variables.Select(v => v.Name));
        Assert.Equal(2097152, spec.Settings.FunctionExecutionLimitMemoryBytes);
        Assert.Equal(2, spec.RoutingConfig.Routes.Count);
        Assert.Empty(spec.ApiCorsSettings.AllowedOrigins);
        var persistence = await db.Database.SqlQueryRaw<string>(
            "SELECT relpersistence::text AS \"Value\" FROM pg_class WHERE oid = 'app_specifications'::regclass")
            .SingleAsync();
        Assert.Equal("p", persistence);
        // A rebuild is repeatable and never needs the old cache.
        Assert.Equal(1, await new AppSpecificationStore(db).BackfillAsync());
        Assert.NotNull(await database.Store.GetBySlugAsync("existing"));
    }

    [PostgreSqlFact]
    public async Task Writes_publish_atomically_and_rename_authentication_and_delete_keep_store_consistent()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        var app = new App { Slug = "lifecycle", OwnerId = "owner", State = Api.Domain.Enums.AppState.Active };
        db.Apps.Add(app);
        await db.SaveChangesAsync();
        Assert.NotNull(await database.Store.GetBySlugAsync(app.Slug));

        var deployment = new ApiDeployment { AppId = app.Id, Status = DeploymentStatus.Ready };
        db.ApiDeployments.Add(deployment);
        await db.SaveChangesAsync();
        app.ActiveApiDeploymentId = deployment.Id;
        await db.SaveChangesAsync();
        Assert.Equal(deployment.Id, (await database.Store.GetBySlugAsync(app.Slug))!.ApiDeploymentId);

        db.AuthenticationSchemes.Add(new AuthenticationScheme
        {
            AppId = app.Id, Type = Api.Domain.Enums.AuthenticationSchemeType.OpenIdConnect,
            Enabled = true
        });
        await db.SaveChangesAsync();
        Assert.Single((await database.Store.GetBySlugAsync(app.Slug))!.AuthenticationSchemes);

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            app.State = Api.Domain.Enums.AppState.Disabled;
            await db.SaveChangesAsync();
            // Separate connection sees only committed configuration.
            Assert.Equal(MycroCloud.WebApp.Gateway.Models.AppState.Active,
                (await database.Store.GetBySlugAsync(app.Slug))!.State);
            await transaction.RollbackAsync();
        }
        db.ChangeTracker.Clear();
        app = await db.Apps.SingleAsync();
        app.Slug = "renamed";
        await db.SaveChangesAsync();
        Assert.Null(await database.Store.GetBySlugAsync("lifecycle"));
        Assert.NotNull(await database.Store.GetBySlugAsync("renamed"));

        app.CorsSettings = new CorsSettings { AllowedOrigins = ["https://example.com"],
            AllowedHeaders = [], AllowedMethods = [], ExposeHeaders = [] };
        await db.SaveChangesAsync();
        app.CorsSettings.AllowedOrigins.Add("https://other.example.com");
        await db.SaveChangesAsync();
        Assert.Equal(2, (await database.Store.GetBySlugAsync("renamed"))!.ApiCorsSettings.AllowedOrigins.Count);

        await db.Database.ExecuteSqlRawAsync("DELETE FROM cache_entries");
        Assert.NotNull(await database.Store.GetBySlugAsync("renamed"));
        await new AppRepository(db).Delete(app.Id);
        Assert.Null(await database.Store.GetBySlugAsync("renamed"));
    }

    [PostgreSqlFact]
    public async Task Projection_failure_rolls_back_business_changes()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        db.Apps.Add(new App { Slug = "rollback", OwnerId = "owner", State = Api.Domain.Enums.AppState.Active });
        await db.SaveChangesAsync();
        // Inject a failure after the app write, during projection.
        await db.Database.ExecuteSqlRawAsync("""
            ALTER TABLE app_specifications ADD CONSTRAINT reject_disabled
            CHECK ((specification->>'State')::integer <> 2);
            """);
        var app = await db.Apps.SingleAsync();
        app.State = Api.Domain.Enums.AppState.Disabled;
        await Assert.ThrowsAsync<PostgresException>(() => db.SaveChangesAsync());
        await using var verification = database.CreateContext();
        Assert.Equal(Api.Domain.Enums.AppState.Active, (await verification.Apps.SingleAsync()).State);
        Assert.Equal(MycroCloud.WebApp.Gateway.Models.AppState.Active,
            (await database.Store.GetBySlugAsync("rollback"))!.State);
    }

    [PostgreSqlFact]
    public async Task Concurrent_child_updates_preserve_both_changes()
    {
        await using var database = await TestDatabase.CreateAsync();
        int appId;
        await using (var setup = database.CreateContext())
        {
            var app = new App { Slug = "concurrent", OwnerId = "owner", State = Api.Domain.Enums.AppState.Active };
            setup.Apps.Add(app);
            await setup.SaveChangesAsync();
            appId = app.Id;
        }
        await using var first = database.CreateContext();
        await using var second = database.CreateContext();
        await using var transaction = await first.Database.BeginTransactionAsync();
        first.Variables.Add(new Variable { AppId = appId, Name = "FIRST", Value = "one" });
        await first.SaveChangesAsync();
        second.Variables.Add(new Variable { AppId = appId, Name = "SECOND", Value = "two" });
        // This save must wait for the first app-row lock, then project both committed values.
        var pending = second.SaveChangesAsync();
        await transaction.CommitAsync();
        await pending.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(new[] { "FIRST", "SECOND" },
            (await database.Store.GetBySlugAsync("concurrent"))!.Variables.Select(v => v.Name));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly string connectionString;
        private readonly string schema;
        private readonly NpgsqlDataSource dataSource;
        public PostgresAppSpecificationStore Store { get; }

        private TestDatabase(string connectionString, string schema)
        {
            this.connectionString = connectionString;
            this.schema = schema;
            dataSource = NpgsqlDataSource.Create(new NpgsqlConnectionStringBuilder(connectionString)
                { SearchPath = schema }.ConnectionString);
            Store = new PostgresAppSpecificationStore(dataSource);
        }

        public ApiDbContext CreateContext() => new(new DbContextOptionsBuilder<ApiDbContext>()
            .UseNpgsql(dataSource, o => o.MigrationsAssembly("Api.Migrations")).Options);

        public static async Task<TestDatabase> CreateAsync(bool migrate = true)
        {
            var database = new TestDatabase(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")!,
                "spec_test_" + Guid.NewGuid().ToString("N"));
            await using var connection = new NpgsqlConnection(database.connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE SCHEMA {database.schema}", connection);
            await command.ExecuteNonQueryAsync();
            try
            {
                if (migrate)
                {
                    await using var db = database.CreateContext();
                    await db.Database.MigrateAsync();
                }
                return database;
            }
            catch
            {
                await database.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await dataSource.DisposeAsync();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP SCHEMA {schema} CASCADE", connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
