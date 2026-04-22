using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class AddingReadReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeedbackCommentReadReceipt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FeedbackCommentId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackCommentReadReceipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackCommentReadReceipt_FeedbackComment_FeedbackCommentId",
                        column: x => x.FeedbackCommentId,
                        principalTable: "FeedbackComment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackCommentReadReceipt_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FeedbackReadReceipt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FeedbackId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackReadReceipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackReadReceipt_Feedback_FeedbackId",
                        column: x => x.FeedbackId,
                        principalTable: "Feedback",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackReadReceipt_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCommentReadReceipt_FeedbackCommentId",
                table: "FeedbackCommentReadReceipt",
                column: "FeedbackCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackCommentReadReceipt_UserId",
                table: "FeedbackCommentReadReceipt",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReadReceipt_FeedbackId",
                table: "FeedbackReadReceipt",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackReadReceipt_UserId",
                table: "FeedbackReadReceipt",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedbackCommentReadReceipt");

            migrationBuilder.DropTable(
                name: "FeedbackReadReceipt");
        }
    }
}
