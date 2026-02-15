using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class DropRouteStaticFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteStaticFiles_Routes_RouteId",
                table: "RouteStaticFiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RouteStaticFiles",
                table: "RouteStaticFiles");

            migrationBuilder.RenameTable(
                name: "RouteStaticFiles",
                newName: "RouteStaticFile");

            migrationBuilder.RenameIndex(
                name: "IX_RouteStaticFiles_RouteId",
                table: "RouteStaticFile",
                newName: "IX_RouteStaticFile_RouteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RouteStaticFile",
                table: "RouteStaticFile",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStaticFile_Routes_RouteId",
                table: "RouteStaticFile",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RouteStaticFile_Routes_RouteId",
                table: "RouteStaticFile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RouteStaticFile",
                table: "RouteStaticFile");

            migrationBuilder.RenameTable(
                name: "RouteStaticFile",
                newName: "RouteStaticFiles");

            migrationBuilder.RenameIndex(
                name: "IX_RouteStaticFile_RouteId",
                table: "RouteStaticFiles",
                newName: "IX_RouteStaticFiles_RouteId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RouteStaticFiles",
                table: "RouteStaticFiles",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteStaticFiles_Routes_RouteId",
                table: "RouteStaticFiles",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
