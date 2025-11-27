using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteLogFunctionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalLogMessage",
                table: "Logs");

            migrationBuilder.AddColumn<string>(
                name: "FunctionLogs",
                table: "Logs",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FunctionLogs",
                table: "Logs");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalLogMessage",
                table: "Logs",
                type: "text",
                nullable: true);
        }
    }
}
