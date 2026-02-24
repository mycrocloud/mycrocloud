using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCacheEntriesUnloggedTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE UNLOGGED TABLE cache_entries (
                    key TEXT PRIMARY KEY,
                    value BYTEA NOT NULL,
                    expires_at TIMESTAMPTZ NULL
                );

                CREATE INDEX ix_cache_entries_expires_at ON cache_entries (expires_at) WHERE expires_at IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS cache_entries;");
        }
    }
}
