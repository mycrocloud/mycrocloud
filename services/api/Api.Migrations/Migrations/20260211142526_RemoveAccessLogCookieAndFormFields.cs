using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccessLogCookieAndFormFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Apps_AppId",
                table: "Logs");

            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_AppId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestCookie",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "RequestFormContent",
                table: "Logs");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs");

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

            migrationBuilder.CreateIndex(
                name: "IX_Logs_AppId",
                table: "Logs",
                column: "AppId");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Apps_AppId",
                table: "Logs",
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
        }
    }
}
