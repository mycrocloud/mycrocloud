using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InsertSystemApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string sql =
                """
                INSERT INTO public."Apps" ("Id", "UserId", "Name", "Description", "Status", "CreatedAt", "UpdatedAt", "CorsSettings", "Settings", "Version", "GitHubRepoFullName", "GitHubWebhookToken") VALUES (0, '3lh3FFSWS5VWnDeX26rC0Kvza1HJ70s7@clients', 'mycrocloud', 'mycrocloud', 1, '2024-10-26 19:24:26.041000 +00:00', null, null, null, '00000000-0000-0000-0000-000000000000', null, null);
                """;
            
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
