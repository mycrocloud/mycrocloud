using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations;

public partial class AddDurableAppSpecifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE app_specifications (
                slug text PRIMARY KEY,
                app_id integer NOT NULL UNIQUE REFERENCES "Apps" ("Id") ON DELETE CASCADE,
                specification jsonb NOT NULL,
                version uuid NOT NULL,
                updated_at timestamptz NOT NULL
            );

            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE app_specifications;
            """);
    }
}
