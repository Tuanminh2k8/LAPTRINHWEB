using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Source.Models;

#nullable disable

namespace Source.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260815000000_AddOrderDetailProductSnapshots")]
    public partial class AddOrderDetailProductSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetailModifiers_ModifierOptions_ModifierOptionId",
                table: "OrderDetailModifiers");

            migrationBuilder.AlterColumn<int>(
                name: "ModifierOptionId",
                table: "OrderDetailModifiers",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetailModifiers_ModifierOptions_ModifierOptionId",
                table: "OrderDetailModifiers",
                column: "ModifierOptionId",
                principalTable: "ModifierOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddColumn<string>(
                name: "ProductDescription",
                table: "OrderDetails",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductImageUrl",
                table: "OrderDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE od
                SET FastFoodName = COALESCE(od.FastFoodName, f.Name, c.Name),
                    ProductImageUrl = COALESCE(f.ImageUrl, c.ImageUrl, '/images/default_food.jpg'),
                    ProductDescription = COALESCE(f.Description, c.Description, '')
                FROM OrderDetails od
                LEFT JOIN FastFoods f ON f.Id = od.FastFoodId
                LEFT JOIN Combos c ON c.Id = od.ComboId;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetailModifiers_ModifierOptions_ModifierOptionId",
                table: "OrderDetailModifiers");

            migrationBuilder.Sql("DELETE FROM OrderDetailModifiers WHERE ModifierOptionId IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "ModifierOptionId",
                table: "OrderDetailModifiers",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetailModifiers_ModifierOptions_ModifierOptionId",
                table: "OrderDetailModifiers",
                column: "ModifierOptionId",
                principalTable: "ModifierOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(name: "ProductDescription", table: "OrderDetails");
            migrationBuilder.DropColumn(name: "ProductImageUrl", table: "OrderDetails");
        }
    }
}
