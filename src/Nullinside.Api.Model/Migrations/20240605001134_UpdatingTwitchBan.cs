using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class UpdatingTwitchBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TwitchBan_TwitchUser_BannedUserId",
                table: "TwitchBan");

            migrationBuilder.DropForeignKey(
                name: "FK_TwitchBan_Users_ChannelId",
                table: "TwitchBan");

            migrationBuilder.DropIndex(
                name: "IX_TwitchBan_BannedUserId",
                table: "TwitchBan");

            migrationBuilder.DropIndex(
                name: "IX_TwitchBan_ChannelId",
                table: "TwitchBan");

            migrationBuilder.DropColumn(
                name: "BannedUserId",
                table: "TwitchBan");

            migrationBuilder.AlterColumn<string>(
                name: "ChannelId",
                table: "TwitchBan",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "BannedUserTwitchId",
                table: "TwitchBan",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "TwitchBan",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "TwitchBan",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BannedUserTwitchId",
                table: "TwitchBan");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "TwitchBan");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "TwitchBan");

            migrationBuilder.AlterColumn<int>(
                name: "ChannelId",
                table: "TwitchBan",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<int>(
                name: "BannedUserId",
                table: "TwitchBan",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TwitchBan_BannedUserId",
                table: "TwitchBan",
                column: "BannedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TwitchBan_ChannelId",
                table: "TwitchBan",
                column: "ChannelId");

            migrationBuilder.AddForeignKey(
                name: "FK_TwitchBan_TwitchUser_BannedUserId",
                table: "TwitchBan",
                column: "BannedUserId",
                principalTable: "TwitchUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TwitchBan_Users_ChannelId",
                table: "TwitchBan",
                column: "ChannelId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
