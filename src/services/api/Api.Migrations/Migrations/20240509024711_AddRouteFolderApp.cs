using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteFolderApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppId",
                table: "RouteFolders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RouteFolders_AppId",
                table: "RouteFolders",
                column: "AppId");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteFolders_Apps_AppId",
                table: "RouteFolders",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteFolders_Apps_AppId",
                table: "RouteFolders");

            migrationBuilder.DropIndex(
                name: "IX_RouteFolders_AppId",
                table: "RouteFolders");

            migrationBuilder.DropColumn(
                name: "AppId",
                table: "RouteFolders");
        }
    }
}
