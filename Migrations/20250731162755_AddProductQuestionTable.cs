using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTKETicaretSitesi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductQuestionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductReviewAnalyses_ProductId",
                table: "ProductReviewAnalyses");

            migrationBuilder.CreateTable(
                name: "ProductQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnswerDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SellerId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_AspNetUsers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductQuestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewAnalyses_ProductId",
                table: "ProductReviewAnalyses",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_ProductId",
                table: "ProductQuestions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_SellerId",
                table: "ProductQuestions",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_UserId",
                table: "ProductQuestions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductQuestions");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviewAnalyses_ProductId",
                table: "ProductReviewAnalyses");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewAnalyses_ProductId",
                table: "ProductReviewAnalyses",
                column: "ProductId");
        }
    }
}
