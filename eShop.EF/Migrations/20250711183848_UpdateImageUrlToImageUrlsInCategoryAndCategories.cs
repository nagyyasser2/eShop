using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.EF.Migrations
{
    /// <inheritdoc />
    public partial class UpdateImageUrlToImageUrlsInCategoryAndCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "SubCategories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "SubCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "SubCategories");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "SubCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
