using Api.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Api.Migrations.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260216100000_CleanupLegacyRouteResponseColumns")]
public class CleanupLegacyRouteResponseColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "FunctionHandlerDependencies",
            table: "Routes");

        migrationBuilder.DropColumn(
            name: "FunctionHandlerMethod",
            table: "Routes");

        migrationBuilder.DropColumn(
            name: "ResponseBodyLanguage",
            table: "Routes");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string[]>(
            name: "FunctionHandlerDependencies",
            table: "Routes",
            type: "text[]",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "FunctionHandlerMethod",
            table: "Routes",
            type: "text",
            nullable: true,
            defaultValue: "handler");

        migrationBuilder.AddColumn<string>(
            name: "ResponseBodyLanguage",
            table: "Routes",
            type: "text",
            nullable: true);
    }
}
