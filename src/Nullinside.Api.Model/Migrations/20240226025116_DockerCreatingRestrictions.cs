using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class DockerCreatingRestrictions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DockerDeployments_DisplayName",
                table: "DockerDeployments",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_DockerDeployments_IsDockerComposeProject_Name",
                table: "DockerDeployments",
                columns: new[] { "IsDockerComposeProject", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DockerDeployments_DisplayName",
                table: "DockerDeployments");

            migrationBuilder.DropIndex(
                name: "IX_DockerDeployments_IsDockerComposeProject_Name",
                table: "DockerDeployments");
        }
    }
}
