using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameTokenToHashedToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiTokens",
                table: "ApiTokens");

            migrationBuilder.RenameColumn(
                name: "Token",
                table: "ApiTokens",
                newName: "HashedToken");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ApiTokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiTokens",
                table: "ApiTokens",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiTokens",
                table: "ApiTokens");

            migrationBuilder.RenameColumn(
                name: "HashedToken",
                table: "ApiTokens",
                newName: "Token");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "ApiTokens",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiTokens",
                table: "ApiTokens",
                column: "Token");
        }
    }
}
