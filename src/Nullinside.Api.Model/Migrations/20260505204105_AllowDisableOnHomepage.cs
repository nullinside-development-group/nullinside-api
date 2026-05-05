using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class AllowDisableOnHomepage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnHomePage",
                table: "TwitchUserConfig",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
            
            migrationBuilder.Sql("UPDATE TwitchUserConfig SET ShowOnHomePage = 1 WHERE 1=1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowOnHomePage",
                table: "TwitchUserConfig");
        }
    }
}
