using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameFunctionRuntimeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FunctionExecutionEnvironment",
                table: "Routes",
                newName: "FunctionRuntime");

            migrationBuilder.RenameColumn(
                name: "FunctionExecutionEnvironment",
                table: "Logs",
                newName: "FunctionRuntime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FunctionRuntime",
                table: "Routes",
                newName: "FunctionExecutionEnvironment");

            migrationBuilder.RenameColumn(
                name: "FunctionRuntime",
                table: "Logs",
                newName: "FunctionExecutionEnvironment");
        }
    }
}
