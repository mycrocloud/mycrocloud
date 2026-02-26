using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureRouteFileDeleteBehavior : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Files_FileId",
                table: "Routes");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Files_FileId",
                table: "Routes",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Files_FileId",
                table: "Routes");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Files_FileId",
                table: "Routes",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");
        }
    }
}
