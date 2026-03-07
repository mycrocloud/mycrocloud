using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFunctionExecutionEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FunctionExecutionEnvironment",
                table: "Routes",
                type: "integer",
                nullable: true);

            const string sql = @"
UPDATE ""Routes"" SET ""FunctionExecutionEnvironment"" = 1 WHERE ""ResponseType"" = 3;
";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FunctionExecutionEnvironment",
                table: "Routes");
        }
    }
}
