using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddLogParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestContentLength",
                table: "Logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestContentType",
                table: "Logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestCookie",
                table: "Logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestFormContent",
                table: "Logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHeaders",
                table: "Logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestPayload",
                table: "Logs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestContentLength",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestContentType",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestCookie",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestFormContent",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestHeaders",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestPayload",
                table: "Logs");
        }
    }
}
