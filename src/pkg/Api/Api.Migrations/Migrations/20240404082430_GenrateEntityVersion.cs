using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class GenrateEntityVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string sql = 
"""
UPDATE public."{0}" SET "Version" = gen_random_uuid();
""";
            string[] tables = ["Apps"];
            foreach (var table in tables)
            {
                migrationBuilder.Sql(string.Format(sql, table));
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
