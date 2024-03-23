using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class Twitch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.Sql("""
                ALTER TABLE Users
                RENAME COLUMN Gmail TO Email;
            """);

            migrationBuilder.RenameIndex(
                name: "IX_Users_Gmail",
                table: "Users",
                newName: "IX_Users_Email");

            migrationBuilder.AddColumn<string>(
                name: "TwitchRefreshToken",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwitchToken",
                table: "Users",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TwitchTokenExpiration",
                table: "Users",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TwitchRefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwitchToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwitchTokenExpiration",
                table: "Users");
            
            migrationBuilder.Sql("""
                ALTER TABLE Users
                RENAME COLUMN Email TO Gmail;
            """);

            migrationBuilder.RenameIndex(
                name: "IX_Users_Email",
                table: "Users",
                newName: "IX_Users_Gmail");
        }
    }
}
