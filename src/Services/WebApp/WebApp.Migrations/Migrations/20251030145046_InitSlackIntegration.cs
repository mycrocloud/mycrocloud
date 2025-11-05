using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitSlackIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlackInstallations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TeamName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BotUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    BotAccessToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Scopes = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    InstalledByUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    EnterpriseId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsEnterpriseInstall = table.Column<bool>(type: "boolean", nullable: false),
                    InstalledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackInstallations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlackInstallations_TeamId",
                table: "SlackInstallations",
                column: "TeamId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlackInstallations");
        }
    }
}
