using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolderId",
                table: "Routes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RouteFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteFolders_RouteFolders_ParentId",
                        column: x => x.ParentId,
                        principalTable: "RouteFolders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_FolderId",
                table: "Routes",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteFolders_ParentId",
                table: "RouteFolders",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Routes_RouteFolders_FolderId",
                table: "Routes",
                column: "FolderId",
                principalTable: "RouteFolders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Routes_RouteFolders_FolderId",
                table: "Routes");

            migrationBuilder.DropTable(
                name: "RouteFolders");

            migrationBuilder.DropIndex(
                name: "IX_Routes_FolderId",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "Routes");
        }
    }
}
