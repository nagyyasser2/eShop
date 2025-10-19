using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eShop.EF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsAttachedFiled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IDeletable",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "IsAttached",
                table: "ProductImages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IDeletable",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAttached",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
