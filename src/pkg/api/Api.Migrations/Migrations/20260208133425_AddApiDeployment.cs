using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddApiDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentFiles_SpaDeployments_DeploymentId",
                table: "DeploymentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Releases_SpaDeployments_SpaDeploymentId",
                table: "Releases");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaDeployments_AppBuildJobs_BuildId",
                table: "SpaDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaDeployments_Apps_AppId",
                table: "SpaDeployments");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaDeployments_Artifacts_ArtifactId",
                table: "SpaDeployments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SpaDeployments",
                table: "SpaDeployments");

            migrationBuilder.RenameTable(
                name: "SpaDeployments",
                newName: "Deployments");

            migrationBuilder.RenameIndex(
                name: "IX_SpaDeployments_BuildId",
                table: "Deployments",
                newName: "IX_Deployments_BuildId");

            migrationBuilder.RenameIndex(
                name: "IX_SpaDeployments_ArtifactId",
                table: "Deployments",
                newName: "IX_Deployments_ArtifactId");

            migrationBuilder.RenameIndex(
                name: "IX_SpaDeployments_AppId_Status",
                table: "Deployments",
                newName: "IX_Deployments_AppId_Status");

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveApiDeploymentId",
                table: "Apps",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ArtifactId",
                table: "Deployments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "DeploymentType",
                table: "Deployments",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Deployments",
                table: "Deployments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_ActiveApiDeploymentId",
                table: "Apps",
                column: "ActiveApiDeploymentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_Deployments_ActiveApiDeploymentId",
                table: "Apps",
                column: "ActiveApiDeploymentId",
                principalTable: "Deployments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentFiles_Deployments_DeploymentId",
                table: "DeploymentFiles",
                column: "DeploymentId",
                principalTable: "Deployments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_AppBuildJobs_BuildId",
                table: "Deployments",
                column: "BuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_Apps_AppId",
                table: "Deployments",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Deployments_Artifacts_ArtifactId",
                table: "Deployments",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Releases_Deployments_SpaDeploymentId",
                table: "Releases",
                column: "SpaDeploymentId",
                principalTable: "Deployments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apps_Deployments_ActiveApiDeploymentId",
                table: "Apps");

            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentFiles_Deployments_DeploymentId",
                table: "DeploymentFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_AppBuildJobs_BuildId",
                table: "Deployments");

            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_Apps_AppId",
                table: "Deployments");

            migrationBuilder.DropForeignKey(
                name: "FK_Deployments_Artifacts_ArtifactId",
                table: "Deployments");

            migrationBuilder.DropForeignKey(
                name: "FK_Releases_Deployments_SpaDeploymentId",
                table: "Releases");

            migrationBuilder.DropIndex(
                name: "IX_Apps_ActiveApiDeploymentId",
                table: "Apps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Deployments",
                table: "Deployments");

            migrationBuilder.DropColumn(
                name: "ActiveApiDeploymentId",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "DeploymentType",
                table: "Deployments");

            migrationBuilder.RenameTable(
                name: "Deployments",
                newName: "SpaDeployments");

            migrationBuilder.RenameIndex(
                name: "IX_Deployments_BuildId",
                table: "SpaDeployments",
                newName: "IX_SpaDeployments_BuildId");

            migrationBuilder.RenameIndex(
                name: "IX_Deployments_ArtifactId",
                table: "SpaDeployments",
                newName: "IX_SpaDeployments_ArtifactId");

            migrationBuilder.RenameIndex(
                name: "IX_Deployments_AppId_Status",
                table: "SpaDeployments",
                newName: "IX_SpaDeployments_AppId_Status");

            migrationBuilder.AlterColumn<Guid>(
                name: "ArtifactId",
                table: "SpaDeployments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpaDeployments",
                table: "SpaDeployments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentFiles_SpaDeployments_DeploymentId",
                table: "DeploymentFiles",
                column: "DeploymentId",
                principalTable: "SpaDeployments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Releases_SpaDeployments_SpaDeploymentId",
                table: "Releases",
                column: "SpaDeploymentId",
                principalTable: "SpaDeployments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SpaDeployments_AppBuildJobs_BuildId",
                table: "SpaDeployments",
                column: "BuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SpaDeployments_Apps_AppId",
                table: "SpaDeployments",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SpaDeployments_Artifacts_ArtifactId",
                table: "SpaDeployments",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
