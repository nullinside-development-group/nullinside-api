using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class DockerDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "DockerDeployments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "DockerDeployments");
        }
    }
}
