using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RefactoringAppLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppIntegration");

            migrationBuilder.AddColumn<string>(
                name: "BuildConfigs",
                table: "Apps",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AppLink",
                columns: table => new
                {
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    InstallationId = table.Column<long>(type: "bigint", nullable: false),
                    RepoId = table.Column<long>(type: "bigint", nullable: false),
                    RepoName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLink", x => x.AppId);
                    table.ForeignKey(
                        name: "FK_AppLink_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppLink_GitHubInstallations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "GitHubInstallations",
                        principalColumn: "InstallationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLink_InstallationId",
                table: "AppLink",
                column: "InstallationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLink");

            migrationBuilder.DropColumn(
                name: "BuildConfigs",
                table: "Apps");

            migrationBuilder.CreateTable(
                name: "AppIntegration",
                columns: table => new
                {
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    InstallationId = table.Column<long>(type: "bigint", nullable: false),
                    Branch = table.Column<string>(type: "text", nullable: true),
                    BuildCommand = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Directory = table.Column<string>(type: "text", nullable: true),
                    InstallCommand = table.Column<string>(type: "text", nullable: true),
                    OutDir = table.Column<string>(type: "text", nullable: true),
                    RepoId = table.Column<long>(type: "bigint", nullable: false),
                    RepoName = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppIntegration", x => x.AppId);
                    table.ForeignKey(
                        name: "FK_AppIntegration_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AppIntegration_GitHubInstallations_InstallationId",
                        column: x => x.InstallationId,
                        principalTable: "GitHubInstallations",
                        principalColumn: "InstallationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppIntegration_InstallationId",
                table: "AppIntegration",
                column: "InstallationId");
        }
    }
}
