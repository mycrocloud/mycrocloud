using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AlterRouteResponseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string sql = 
"""
ALTER TABLE "Routes" ALTER COLUMN "ResponseType" TYPE integer USING (
    CASE "ResponseType"
        WHEN 'static' THEN 1
        WHEN 'staticFile' THEN 2
        WHEN 'function' THEN 3
    END
);
""";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
