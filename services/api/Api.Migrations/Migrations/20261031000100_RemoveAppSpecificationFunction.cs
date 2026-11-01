using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations;

public partial class RemoveAppSpecificationFunction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
        => migrationBuilder.Sql("DROP FUNCTION IF EXISTS refresh_app_specification(integer);");

    // Application code owns publishing; rolling back this cleanup does not reinstall business logic.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
