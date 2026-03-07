using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddOnDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Apps_AppId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteFolders_Apps_AppId",
                table: "RouteFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_RouteFolders_FolderId",
                table: "Routes");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Apps_AppId",
                table: "ApiKeys",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RouteFolders_Apps_AppId",
                table: "RouteFolders",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_RouteFolders_FolderId",
                table: "Routes",
                column: "FolderId",
                principalTable: "RouteFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_Apps_AppId",
                table: "ApiKeys");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs");

            migrationBuilder.DropForeignKey(
                name: "FK_RouteFolders_Apps_AppId",
                table: "RouteFolders");

            migrationBuilder.DropForeignKey(
                name: "FK_Routes_RouteFolders_FolderId",
                table: "Routes");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_Apps_AppId",
                table: "ApiKeys",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RouteFolders_Apps_AppId",
                table: "RouteFolders",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_RouteFolders_FolderId",
                table: "Routes",
                column: "FolderId",
                principalTable: "RouteFolders",
                principalColumn: "Id");
        }
    }
}
