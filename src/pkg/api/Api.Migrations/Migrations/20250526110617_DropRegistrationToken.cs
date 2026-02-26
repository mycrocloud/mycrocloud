using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class DropRegistrationToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunnerRegistrationTokens");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunnerRegistrationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AppId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunnerRegistrationTokens", x => x.Id);
                    table.CheckConstraint("CK_RunnerRegistrationToken_Scope_Requirement", "(\"Scope\" = 1 AND \"UserId\" IS NOT NULL AND \"AppId\" IS NULL) OR\n(\"Scope\" = 2 AND \"UserId\" IS NULL AND \"AppId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_RunnerRegistrationTokens_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRegistrationTokens_AppId",
                table: "RunnerRegistrationTokens",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_RunnerRegistrationTokens_Token",
                table: "RunnerRegistrationTokens",
                column: "Token",
                unique: true);
        }
    }
}
