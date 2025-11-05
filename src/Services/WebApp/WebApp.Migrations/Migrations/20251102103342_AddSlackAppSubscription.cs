using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSlackAppSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SlackAppSubscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SlackUserId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AppId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlackAppSubscriptions", x => x.SubscriptionId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlackAppSubscriptions");
        }
    }
}
