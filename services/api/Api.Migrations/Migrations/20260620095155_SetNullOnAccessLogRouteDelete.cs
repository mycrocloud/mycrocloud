using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class SetNullOnAccessLogRouteDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs");

            migrationBuilder.AddForeignKey(
                name: "FK_Logs_Routes_RouteId",
                table: "Logs",
                column: "RouteId",
                principalTable: "Routes",
                principalColumn: "Id");
        }
    }
}
