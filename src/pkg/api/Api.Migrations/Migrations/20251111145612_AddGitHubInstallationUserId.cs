using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddGitHubInstallationUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GitHubInstallations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GitHubInstallations_UserId",
                table: "GitHubInstallations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GitHubInstallations_UserId",
                table: "GitHubInstallations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GitHubInstallations");
        }
    }
}
