using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReleaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apps_Releases_ActiveReleaseId",
                table: "Apps");

            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.RenameColumn(
                name: "ActiveReleaseId",
                table: "Apps",
                newName: "ActiveSpaDeploymentId");

            migrationBuilder.RenameIndex(
                name: "IX_Apps_ActiveReleaseId",
                table: "Apps",
                newName: "IX_Apps_ActiveSpaDeploymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_Deployments_ActiveSpaDeploymentId",
                table: "Apps",
                column: "ActiveSpaDeploymentId",
                principalTable: "Deployments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apps_Deployments_ActiveSpaDeploymentId",
                table: "Apps");

            migrationBuilder.RenameColumn(
                name: "ActiveSpaDeploymentId",
                table: "Apps",
                newName: "ActiveReleaseId");

            migrationBuilder.RenameIndex(
                name: "IX_Apps_ActiveSpaDeploymentId",
                table: "Apps",
                newName: "IX_Apps_ActiveReleaseId");

            migrationBuilder.CreateTable(
                name: "Releases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    SpaDeploymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Releases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Releases_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Releases_Deployments_SpaDeploymentId",
                        column: x => x.SpaDeploymentId,
                        principalTable: "Deployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Releases_AppId",
                table: "Releases",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_SpaDeploymentId",
                table: "Releases",
                column: "SpaDeploymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_Releases_ActiveReleaseId",
                table: "Apps",
                column: "ActiveReleaseId",
                principalTable: "Releases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
