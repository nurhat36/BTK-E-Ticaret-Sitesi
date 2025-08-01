using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTKETicaretSitesi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductQuestionAnalysisTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductQuestionAnalysis_Products_ProductId",
                table: "ProductQuestionAnalysis");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductQuestionAnalysis",
                table: "ProductQuestionAnalysis");

            migrationBuilder.RenameTable(
                name: "ProductQuestionAnalysis",
                newName: "ProductQuestionAnalyses");

            migrationBuilder.RenameIndex(
                name: "IX_ProductQuestionAnalysis_ProductId",
                table: "ProductQuestionAnalyses",
                newName: "IX_ProductQuestionAnalyses_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductQuestionAnalyses",
                table: "ProductQuestionAnalyses",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductQuestionAnalyses_Products_ProductId",
                table: "ProductQuestionAnalyses",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductQuestionAnalyses_Products_ProductId",
                table: "ProductQuestionAnalyses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductQuestionAnalyses",
                table: "ProductQuestionAnalyses");

            migrationBuilder.RenameTable(
                name: "ProductQuestionAnalyses",
                newName: "ProductQuestionAnalysis");

            migrationBuilder.RenameIndex(
                name: "IX_ProductQuestionAnalyses_ProductId",
                table: "ProductQuestionAnalysis",
                newName: "IX_ProductQuestionAnalysis_ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductQuestionAnalysis",
                table: "ProductQuestionAnalysis",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductQuestionAnalysis_Products_ProductId",
                table: "ProductQuestionAnalysis",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
