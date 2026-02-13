using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Alter_App_GitHubRepoId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubRepoId",
                table: "Apps");

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepoFullName",
                table: "Apps",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GitHubRepoFullName",
                table: "Apps");

            migrationBuilder.AddColumn<int>(
                name: "GitHubRepoId",
                table: "Apps",
                type: "integer",
                nullable: true);
        }
    }
}
