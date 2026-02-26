using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameVariableStringValueToValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueType",
                table: "Variables");

            migrationBuilder.RenameColumn(
                name: "StringValue",
                table: "Variables",
                newName: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "Variables",
                newName: "StringValue");

            migrationBuilder.AddColumn<int>(
                name: "ValueType",
                table: "Variables",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
