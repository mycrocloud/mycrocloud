using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitAppRootFolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string sql = 
"""
INSERT 
INTO
  public."Folders" ("AppId", "Name", "ParentId", "CreatedAt")
SELECT
  a."Id",
  '/',
  NULL,
  NOW()
FROM
  public."Apps" a
  LEFT JOIN public."Folders" f ON f."AppId" = a."Id"
  AND f."ParentId" IS NULL
""";
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
