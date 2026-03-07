using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RefactoringSpaDeployment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpaDeployments_Artifacts_ArtifactId",
                table: "SpaDeployments");

            migrationBuilder.DropIndex(
                name: "IX_Artifacts_ContentHash",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "BlobData",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Artifacts");

            migrationBuilder.RenameColumn(
                name: "SizeBytes",
                table: "Artifacts",
                newName: "BundleSize");

            migrationBuilder.RenameColumn(
                name: "ArtifactType",
                table: "Artifacts",
                newName: "StorageType");

            migrationBuilder.AddColumn<string>(
                name: "ExtractedPath",
                table: "SpaDeployments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Compression",
                table: "Artifacts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BuildId",
                table: "Artifacts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleHash",
                table: "Artifacts",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "Artifacts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ObjectBlobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StorageType = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BlobData = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectBlobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BlobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ETag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentFiles_ObjectBlobs_BlobId",
                        column: x => x.BlobId,
                        principalTable: "ObjectBlobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeploymentFiles_SpaDeployments_DeploymentId",
                        column: x => x.DeploymentId,
                        principalTable: "SpaDeployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_BuildId",
                table: "Artifacts",
                column: "BuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_BundleHash",
                table: "Artifacts",
                column: "BundleHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentFiles_BlobId",
                table: "DeploymentFiles",
                column: "BlobId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentFiles_DeploymentId",
                table: "DeploymentFiles",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentFiles_DeploymentId_Path",
                table: "DeploymentFiles",
                columns: new[] { "DeploymentId", "Path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectBlobs_ContentHash",
                table: "ObjectBlobs",
                column: "ContentHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Artifacts_AppBuildJobs_BuildId",
                table: "Artifacts",
                column: "BuildId",
                principalTable: "AppBuildJobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SpaDeployments_Artifacts_ArtifactId",
                table: "SpaDeployments",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Artifacts_AppBuildJobs_BuildId",
                table: "Artifacts");

            migrationBuilder.DropForeignKey(
                name: "FK_SpaDeployments_Artifacts_ArtifactId",
                table: "SpaDeployments");

            migrationBuilder.DropTable(
                name: "DeploymentFiles");

            migrationBuilder.DropTable(
                name: "ObjectBlobs");

            migrationBuilder.DropIndex(
                name: "IX_Artifacts_BuildId",
                table: "Artifacts");

            migrationBuilder.DropIndex(
                name: "IX_Artifacts_BundleHash",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "ExtractedPath",
                table: "SpaDeployments");

            migrationBuilder.DropColumn(
                name: "BuildId",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "BundleHash",
                table: "Artifacts");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "Artifacts");

            migrationBuilder.RenameColumn(
                name: "StorageType",
                table: "Artifacts",
                newName: "ArtifactType");

            migrationBuilder.RenameColumn(
                name: "BundleSize",
                table: "Artifacts",
                newName: "SizeBytes");

            migrationBuilder.AlterColumn<string>(
                name: "Compression",
                table: "Artifacts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "BlobData",
                table: "Artifacts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Artifacts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Artifacts_ContentHash",
                table: "Artifacts",
                column: "ContentHash");

            migrationBuilder.AddForeignKey(
                name: "FK_SpaDeployments_Artifacts_ArtifactId",
                table: "SpaDeployments",
                column: "ArtifactId",
                principalTable: "Artifacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
