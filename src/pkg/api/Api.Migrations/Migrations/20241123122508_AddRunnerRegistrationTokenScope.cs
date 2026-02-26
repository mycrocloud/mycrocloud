using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRunnerRegistrationTokenScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RunnerRegistrationTokens_Apps_AppId",
                table: "RunnerRegistrationTokens");

            migrationBuilder.AlterColumn<int>(
                name: "AppId",
                table: "RunnerRegistrationTokens",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "RunnerRegistrationTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RunnerRegistrationTokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RunnerRegistrationTokens_Apps_AppId",
                table: "RunnerRegistrationTokens",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RunnerRegistrationTokens_Apps_AppId",
                table: "RunnerRegistrationTokens");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "RunnerRegistrationTokens");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RunnerRegistrationTokens");

            migrationBuilder.AlterColumn<int>(
                name: "AppId",
                table: "RunnerRegistrationTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RunnerRegistrationTokens_Apps_AppId",
                table: "RunnerRegistrationTokens",
                column: "AppId",
                principalTable: "Apps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
