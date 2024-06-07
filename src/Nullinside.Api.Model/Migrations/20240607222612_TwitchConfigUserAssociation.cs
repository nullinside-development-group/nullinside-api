using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class TwitchConfigUserAssociation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_TwitchUserConfig_TwitchConfigId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TwitchConfigId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TwitchConfigId",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "TwitchUserConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TwitchUserConfig_UserId",
                table: "TwitchUserConfig",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TwitchUserConfig_Users_UserId",
                table: "TwitchUserConfig",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TwitchUserConfig_Users_UserId",
                table: "TwitchUserConfig");

            migrationBuilder.DropIndex(
                name: "IX_TwitchUserConfig_UserId",
                table: "TwitchUserConfig");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TwitchUserConfig");

            migrationBuilder.AddColumn<int>(
                name: "TwitchConfigId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TwitchConfigId",
                table: "Users",
                column: "TwitchConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_TwitchUserConfig_TwitchConfigId",
                table: "Users",
                column: "TwitchConfigId",
                principalTable: "TwitchUserConfig",
                principalColumn: "Id");
        }
    }
}
