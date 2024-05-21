﻿using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApp.Domain.Entities;

#nullable disable

namespace WebApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string sql = 
                """
                UPDATE "Apps" SET "Settings" = '{0}' WHERE "Settings" IS NULL; 
                """;
            migrationBuilder.Sql(string.Format(sql, JsonSerializer.Serialize(AppSettings.Default)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
