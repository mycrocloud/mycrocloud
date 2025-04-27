using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAppBuildJobName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "AppBuildJobs",
                type: "text",
                nullable: true,
                defaultValue: "build");
            
            const string sql = """
                               UPDATE "AppBuildJobs"
                               SET "Name" = 'build_{' || "CreatedAt"::text || '}'
                               WHERE "Name" IS NULL;
                               """;
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "AppBuildJobs");
        }
    }
}
