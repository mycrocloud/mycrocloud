using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAppLatestBuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LatestBuildId",
                table: "Apps",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Apps_LatestBuildId",
                table: "Apps",
                column: "LatestBuildId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_AppBuildJobs_LatestBuildId",
                table: "Apps",
                column: "LatestBuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apps_AppBuildJobs_LatestBuildId",
                table: "Apps");

            migrationBuilder.DropIndex(
                name: "IX_Apps_LatestBuildId",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "LatestBuildId",
                table: "Apps");
        }
    }
}
