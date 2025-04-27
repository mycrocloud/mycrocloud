using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppBuildJobName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string sql = """
                               UPDATE "AppBuildJobs"
                               SET "Name" = 'build_' || "CreatedAt"::text
                               """;
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
