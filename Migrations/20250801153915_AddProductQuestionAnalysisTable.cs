using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTKETicaretSitesi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductQuestionAnalysisTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductQuestionAnalysis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CommonQuestionsSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommonAnswersSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestionAnalysis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductQuestionAnalysis_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestionAnalysis_ProductId",
                table: "ProductQuestionAnalysis",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductQuestionAnalysis");
        }
    }
}
