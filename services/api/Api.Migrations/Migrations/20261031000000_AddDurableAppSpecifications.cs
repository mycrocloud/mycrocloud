using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations;

public partial class AddDurableAppSpecifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Lock source tables until backfill commits: no gaps while upgrading existing data.
        migrationBuilder.Sql("""
            LOCK TABLE "Apps", "AuthenticationSchemes", "Variables" IN SHARE ROW EXCLUSIVE MODE;
            CREATE TABLE app_specifications (
                slug text PRIMARY KEY,
                app_id integer NOT NULL UNIQUE REFERENCES "Apps" ("Id") ON DELETE CASCADE,
                specification jsonb NOT NULL,
                version uuid NOT NULL,
                updated_at timestamptz NOT NULL
            );

            CREATE FUNCTION refresh_app_specification(p_app_id integer) RETURNS void
            LANGUAGE plpgsql AS $function$
            BEGIN
                -- Serialize publishers with changes to the owning app.
                PERFORM 1 FROM "Apps" WHERE "Id" = p_app_id FOR UPDATE;
                IF NOT FOUND THEN RETURN; END IF;

                INSERT INTO app_specifications (slug, app_id, specification, version, updated_at)
                SELECT a."Slug", a."Id", jsonb_build_object(
                    'Id', a."Id", 'Slug', a."Slug", 'OwnerId', a."OwnerId", 'State', a."State",
                    'SpaDeploymentId', a."ActiveSpaDeploymentId", 'ApiDeploymentId', a."ActiveApiDeploymentId",
                    'Settings', COALESCE(a."Settings", '{"CheckFunctionExecutionLimitMemory":true,"FunctionExecutionLimitMemoryBytes":2097152,"CheckFunctionExecutionTimeout":true,"FunctionExecutionTimeoutSeconds":10,"FunctionUseNoSqlConnection":false}'::jsonb),
                    'ApiCorsSettings', COALESCE(a."CorsSettings", '{"AllowedHeaders":[],"AllowedMethods":[],"AllowedOrigins":[],"ExposeHeaders":[],"MaxAgeSeconds":null}'::jsonb),
                    'RoutingConfig', COALESCE(a."RoutingConfig"::jsonb, '{"SchemaVersion":"1.0","Routes":[{"Priority":1,"Match":{"Type":0,"Path":"/api"},"Target":{"Type":0,"StripPrefix":true}},{"Priority":2,"Match":{"Type":0,"Path":"/"},"Target":{"Type":1,"Fallback":"/index.html"}}]}'::jsonb),
                    'AuthenticationSchemes', COALESCE((SELECT jsonb_agg(jsonb_build_object(
                        'Type', s."Type", 'OpenIdConnectAuthority', s."OpenIdConnectAuthority",
                        'OpenIdConnectAudience', s."OpenIdConnectAudience") ORDER BY s."Id")
                        FROM "AuthenticationSchemes" s WHERE s."AppId" = a."Id" AND s."Enabled"), '[]'::jsonb),
                    'Variables', COALESCE((SELECT jsonb_agg(jsonb_build_object('Name', v."Name", 'Value', v."Value") ORDER BY v."Id")
                        FROM "Variables" v WHERE v."AppId" = a."Id" AND v."Target" IN (0, 2)), '[]'::jsonb)
                ), gen_random_uuid(), clock_timestamp()
                FROM "Apps" a WHERE a."Id" = p_app_id
                ON CONFLICT (app_id) DO UPDATE SET
                    slug = EXCLUDED.slug, specification = EXCLUDED.specification,
                    version = EXCLUDED.version, updated_at = EXCLUDED.updated_at;
            END;
            $function$;

            -- Reconstruct every app, including those whose cache already expired.
            SELECT refresh_app_specification("Id") FROM "Apps" ORDER BY "Id";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP FUNCTION refresh_app_specification(integer);
            DROP TABLE app_specifications;
            """);
    }
}
