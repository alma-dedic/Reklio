using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reklio.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiItemReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_Branch_DocumentNumber_PurchaseDate",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Branch_DocumentNumber_PurchaseDate_ProductId",
                table: "Purchases",
                columns: new[] { "Branch", "DocumentNumber", "PurchaseDate", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_Branch_DocumentNumber_PurchaseDate_ProductId",
                table: "Purchases");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_Branch_DocumentNumber_PurchaseDate",
                table: "Purchases",
                columns: new[] { "Branch", "DocumentNumber", "PurchaseDate" },
                unique: true);
        }
    }
}
