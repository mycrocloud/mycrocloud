using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRunnerRegistrationTokenConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_RunnerRegistrationToken_Scope_Requirement",
                table: "RunnerRegistrationTokens",
                sql: "(\"Scope\" = 1 AND \"UserId\" IS NOT NULL AND \"AppId\" IS NULL) OR\n(\"Scope\" = 2 AND \"UserId\" IS NULL AND \"AppId\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RunnerRegistrationToken_Scope_Requirement",
                table: "RunnerRegistrationTokens");
        }
    }
}
