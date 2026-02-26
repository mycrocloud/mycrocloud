using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestValidationSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteValidations");

            migrationBuilder.AddColumn<string>(
                name: "RequestBodySchema",
                table: "Routes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHeaderSchema",
                table: "Routes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestQuerySchema",
                table: "Routes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestBodySchema",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RequestHeaderSchema",
                table: "Routes");

            migrationBuilder.DropColumn(
                name: "RequestQuerySchema",
                table: "Routes");

            migrationBuilder.CreateTable(
                name: "RouteValidations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteId = table.Column<int>(type: "integer", nullable: false),
                    Expressions = table.Column<List<string>>(type: "text[]", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Rules = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteValidations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteValidations_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteValidations_RouteId",
                table: "RouteValidations",
                column: "RouteId");
        }
    }
}
