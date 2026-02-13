using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RefactoringAppDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear incompatible data before schema restructuring
            migrationBuilder.Sql("DELETE FROM \"AppBuildArtifacts\";");
            migrationBuilder.Sql("UPDATE \"Apps\" SET \"LatestBuildId\" = NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_AppBuildArtifacts_AppBuildJobs_BuildId",
                table: "AppBuildArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Apps_AppBuildJobs_LatestBuildId",
                table: "Apps");

            migrationBuilder.DropIndex(
                name: "IX_Apps_LatestBuildId",
                table: "Apps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppBuildArtifacts",
                table: "AppBuildArtifacts");

            migrationBuilder.DropColumn(
                name: "ContainerId",
                table: "AppBuildJobs");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "AppBuildJobs");

            migrationBuilder.DropColumn(
                name: "Path",
                table: "AppBuildArtifacts");

            migrationBuilder.DropColumn(
                name: "Content",
                table: "AppBuildArtifacts");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Apps",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "Apps",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Apps",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "LatestBuildId",
                table: "Apps",
                newName: "ActiveReleaseId");

            migrationBuilder.RenameIndex(
                name: "IX_Apps_Name",
                table: "Apps",
                newName: "IX_Apps_Slug");

            migrationBuilder.RenameColumn(
                name: "BuildId",
                table: "AppBuildArtifacts",
                newName: "ArtifactId");

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "AppBuildJobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BuildJobId",
                table: "AppBuildArtifacts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AppBuildArtifacts",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppBuildArtifacts",
                table: "AppBuildArtifacts",
                columns: new[] { "BuildJobId", "ArtifactId" });

            migrationBuilder.CreateTable(
                name: "Artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    ArtifactType = table.Column<int>(type: "integer", nullable: false),
                    BlobData = table.Column<byte[]>(type: "bytea", nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Compression = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Artifacts_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpaDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false),
                    BuildId = table.Column<Guid>(type: "uuid", nullable: true),
                    ArtifactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpaDeployments_AppBuildJobs_BuildId",
                        column: x => x.BuildId,
                        principalTable: "AppBuildJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpaDeployments_Apps_AppId",
                        column: x => x.AppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpaDeployments_Artifacts_ArtifactId",
                        column: x => x.ArtifactId,
                        principalTable: "Artifacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                        name: "FK_Releases_SpaDeployments_SpaDeploymentId",
                        column: x => x.SpaDeploymentId,
                        principalTable: "SpaDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apps_ActiveReleaseId",
                table: "Apps",
                column: "ActiveReleaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AppBuildArtifacts_ArtifactId",
                table: "AppBuildArtifacts",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_AppId",
                table: "Artifacts",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_ContentHash",
                table: "Artifacts",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_AppId",
                table: "Releases",
                column: "AppId");

            migrationBuilder.CreateIndex(
                name: "IX_Releases_SpaDeploymentId",
                table: "Releases",
                column: "SpaDeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaDeployments_AppId_Status",
                table: "SpaDeployments",
                columns: new[] { "AppId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SpaDeployments_ArtifactId",
                table: "SpaDeployments",
                column: "ArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaDeployments_BuildId",
                table: "SpaDeployments",
                column: "BuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppBuildArtifacts_AppBuildJobs_BuildJobId",
                table: "AppBuildArtifacts",
                column: "BuildJobId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppBuildArtifacts_Artifacts_ArtifactId",
                table: "AppBuildArtifacts",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_Releases_ActiveReleaseId",
                table: "Apps",
                column: "ActiveReleaseId",
                principalTable: "Releases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppBuildArtifacts_AppBuildJobs_BuildJobId",
                table: "AppBuildArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_AppBuildArtifacts_Artifacts_ArtifactId",
                table: "AppBuildArtifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_Apps_Releases_ActiveReleaseId",
                table: "Apps");

            migrationBuilder.DropTable(
                name: "Releases");

            migrationBuilder.DropTable(
                name: "SpaDeployments");

            migrationBuilder.DropTable(
                name: "Artifacts");

            migrationBuilder.DropIndex(
                name: "IX_Apps_ActiveReleaseId",
                table: "Apps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppBuildArtifacts",
                table: "AppBuildArtifacts");

            migrationBuilder.DropIndex(
                name: "IX_AppBuildArtifacts_ArtifactId",
                table: "AppBuildArtifacts");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "AppBuildJobs");

            migrationBuilder.DropColumn(
                name: "BuildJobId",
                table: "AppBuildArtifacts");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AppBuildArtifacts");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "Apps",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "Apps",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Apps",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ActiveReleaseId",
                table: "Apps",
                newName: "LatestBuildId");

            migrationBuilder.RenameIndex(
                name: "IX_Apps_Slug",
                table: "Apps",
                newName: "IX_Apps_Name");

            migrationBuilder.RenameColumn(
                name: "ArtifactId",
                table: "AppBuildArtifacts",
                newName: "BuildId");

            migrationBuilder.AddColumn<string>(
                name: "ContainerId",
                table: "AppBuildJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AppBuildJobs",
                type: "text",
                nullable: true,
                defaultValue: "build");

            migrationBuilder.AddColumn<string>(
                name: "Path",
                table: "AppBuildArtifacts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "Content",
                table: "AppBuildArtifacts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppBuildArtifacts",
                table: "AppBuildArtifacts",
                columns: new[] { "BuildId", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_Apps_LatestBuildId",
                table: "Apps",
                column: "LatestBuildId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AppBuildArtifacts_AppBuildJobs_BuildId",
                table: "AppBuildArtifacts",
                column: "BuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_AppBuildJobs_LatestBuildId",
                table: "Apps",
                column: "LatestBuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
