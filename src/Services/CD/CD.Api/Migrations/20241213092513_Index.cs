using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CD.Api.Migrations
{
    /// <inheritdoc />
    public partial class Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Domains_Mcrn",
                table: "Domains",
                column: "Mcrn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Domains_Mcrn",
                table: "Domains");
        }
    }
}
