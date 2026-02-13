using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRouteResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ResponseBody",
                table: "Routes",
                newName: "Response");
            
            // Update FunctionHandler to Response if ResponseType is 3 (Function)
            const string sql = """
                               UPDATE "Routes"
                               SET "Response" = "FunctionHandler"
                               WHERE "ResponseType" = 3
                               """;
            
            migrationBuilder.Sql(sql);
            
            migrationBuilder.DropColumn(
                name: "FunctionHandler",
                table: "Routes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Response",
                table: "Routes",
                newName: "ResponseBody");

            migrationBuilder.AddColumn<string>(
                name: "FunctionHandler",
                table: "Routes",
                type: "text",
                nullable: true);
        }
    }
}
