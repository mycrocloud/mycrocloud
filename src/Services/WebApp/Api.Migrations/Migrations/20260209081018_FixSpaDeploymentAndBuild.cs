using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixSpaDeploymentAndBuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AppBuildId",
                table: "Deployments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deployments_AppBuildId",
                table: "Deployments",
                column: "AppBuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_AppBuildJobs_AppBuildId",
                table: "Deployments",
                column: "AppBuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_AppBuildJobs_AppBuildId",
                table: "Deployments");

            migrationBuilder.DropIndex(
                name: "IX_Deployments_AppBuildId",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "AppBuildId",
                table: "Deployments");
        }
    }
}
