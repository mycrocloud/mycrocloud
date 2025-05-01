using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddLogFunctionExecutionEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FunctionExecutionEnvironment",
                table: "Logs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FunctionExecutionEnvironment",
                table: "Logs");
        }
    }
}
