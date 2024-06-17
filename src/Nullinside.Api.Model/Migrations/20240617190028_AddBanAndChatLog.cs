using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddBanAndChatLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TwitchUserBannedOutsideOfBotLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Channel = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    TwitchId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    TwitchUsername = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Reason = table.Column<string>(type: "longtext", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchUserBannedOutsideOfBotLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TwitchUserChatLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Channel = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    TwitchId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    TwitchUsername = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Message = table.Column<string>(type: "longtext", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchUserChatLogs", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwitchUserBannedOutsideOfBotLogs");

            migrationBuilder.DropTable(
                name: "TwitchUserChatLogs");
        }
    }
}
