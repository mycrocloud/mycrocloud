using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLogParams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MachineName",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestPayload",
                table: "Logs");

            migrationBuilder.AlterColumn<long>(
                name: "RequestContentLength",
                table: "Logs",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RequestContentLength",
                table: "Logs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineName",
                table: "Logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestPayload",
                table: "Logs",
                type: "text",
                nullable: true);
        }
    }
}
