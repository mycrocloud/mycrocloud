using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AppIntegrationRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppIntegration_Apps_AppId",
                table: "AppIntegration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppIntegration",
                table: "AppIntegration");

            migrationBuilder.DropIndex(
                name: "IX_AppIntegration_AppId",
                table: "AppIntegration");

            migrationBuilder.DropColumn(
                name: "GitHubRepoFullName",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "GitHubWebhookToken",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AppIntegration");

            migrationBuilder.AddColumn<long>(
                name: "InstallationId",
                table: "AppIntegration",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "RepoId",
                table: "AppIntegration",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "RepoName",
                table: "AppIntegration",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppIntegration",
                table: "AppIntegration",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_AppIntegration_InstallationId",
                table: "AppIntegration",
                column: "InstallationId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppIntegration_Apps_AppId",
                table: "AppIntegration",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppIntegration_GitHubInstallations_InstallationId",
                table: "AppIntegration",
                column: "InstallationId",
                principalTable: "GitHubInstallations",
                principalColumn: "InstallationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppIntegration_Apps_AppId",
                table: "AppIntegration");

            migrationBuilder.DropForeignKey(
                name: "FK_AppIntegration_GitHubInstallations_InstallationId",
                table: "AppIntegration");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppIntegration",
                table: "AppIntegration");

            migrationBuilder.DropIndex(
                name: "IX_AppIntegration_InstallationId",
                table: "AppIntegration");

            migrationBuilder.DropColumn(
                name: "InstallationId",
                table: "AppIntegration");

            migrationBuilder.DropColumn(
                name: "RepoId",
                table: "AppIntegration");

            migrationBuilder.DropColumn(
                name: "RepoName",
                table: "AppIntegration");

            migrationBuilder.AddColumn<string>(
                name: "GitHubRepoFullName",
                table: "Apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GitHubWebhookToken",
                table: "Apps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "AppIntegration",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppIntegration",
                table: "AppIntegration",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AppIntegration_AppId",
                table: "AppIntegration",
                column: "AppId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AppIntegration_Apps_AppId",
                table: "AppIntegration",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
