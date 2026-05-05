using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddingTwitchLiveCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TwitchUserLive",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ViewerCount = table.Column<int>(type: "int", nullable: false),
                    GoneLiveTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    StreamTitle = table.Column<string>(type: "varchar(140)", maxLength: 140, nullable: true),
                    GameName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchUserLive", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TwitchUserLive_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TwitchUserLive_UserId",
                table: "TwitchUserLive",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TwitchUserLive");
        }
    }
}
