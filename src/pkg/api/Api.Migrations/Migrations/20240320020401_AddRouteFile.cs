using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Files_StaticFileId",
                table: "Routes");

            migrationBuilder.RenameColumn(
                name: "StaticFileId",
                table: "Routes",
                newName: "FileId");

            migrationBuilder.RenameIndex(
                name: "IX_Routes_StaticFileId",
                table: "Routes",
                newName: "IX_Routes_FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Files_FileId",
                table: "Routes",
                column: "FileId",
                principalTable: "Files",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_Files_FileId",
                table: "Routes");

            migrationBuilder.RenameColumn(
                name: "FileId",
                table: "Routes",
                newName: "StaticFileId");

            migrationBuilder.RenameIndex(
                name: "IX_Routes_FileId",
                table: "Routes",
                newName: "IX_Routes_StaticFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_Files_StaticFileId",
                table: "Routes",
                column: "StaticFileId",
                principalTable: "Files",
                principalColumn: "Id");
        }
    }
}
