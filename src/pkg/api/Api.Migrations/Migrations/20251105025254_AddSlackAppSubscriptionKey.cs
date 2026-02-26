using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSlackAppSubscriptionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SlackAppSubscriptions",
                table: "SlackAppSubscriptions");

            migrationBuilder.AlterColumn<int>(
                name: "SubscriptionId",
                table: "SlackAppSubscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SlackAppSubscriptions",
                table: "SlackAppSubscriptions",
                columns: new[] { "TeamId", "ChannelId", "AppId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SlackAppSubscriptions",
                table: "SlackAppSubscriptions");

            migrationBuilder.AlterColumn<int>(
                name: "SubscriptionId",
                table: "SlackAppSubscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SlackAppSubscriptions",
                table: "SlackAppSubscriptions",
                column: "SubscriptionId");
        }
    }
}
