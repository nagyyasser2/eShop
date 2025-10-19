using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.EF.Migrations
{
    /// <inheritdoc />
    public partial class RenameSKUtoSkuInProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SKU",
                table: "Products",
                newName: "Sku");

            migrationBuilder.RenameColumn(
                name: "ProductSKU",
                table: "OrderItems",
                newName: "ProductSku");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Sku",
                table: "Products",
                newName: "SKU");

            migrationBuilder.RenameColumn(
                name: "ProductSku",
                table: "OrderItems",
                newName: "ProductSKU");
        }
    }
}
