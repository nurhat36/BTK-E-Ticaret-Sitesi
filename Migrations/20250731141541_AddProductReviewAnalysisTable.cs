using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTKETicaretSitesi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReviewAnalysisTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductReviewAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SentimentAnalysisResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QualityScore = table.Column<double>(type: "float", nullable: false),
                    SummaryInsights = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReviewAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductReviewAnalyses_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviewAnalyses_ProductId",
                table: "ProductReviewAnalyses",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductReviewAnalyses");
        }
    }
}
