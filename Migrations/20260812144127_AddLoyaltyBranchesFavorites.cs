using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Source.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyBranchesFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "ModifierOptions",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "ModifierGroups",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ModifierGroups",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ModifierGroups",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ModifierGroups",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSpent",
                table: "Users",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OpenTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FastFoodId = table.Column<int>(type: "int", nullable: true),
                    ComboId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteItems_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FavoriteItems_FastFoods_FastFoodId",
                        column: x => x.FastFoodId,
                        principalTable: "FastFoods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FavoriteItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PointTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PointTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PointTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/burger?lock=21");

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/pizza?lock=22");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/burger?lock=11");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/burger?lock=12");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 3,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/pizza?lock=13");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/pizza?lock=14");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 5,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/friedchicken?lock=15");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 6,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/fries?lock=16");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 7,
                column: "ImageUrl",
                value: "https://loremflickr.com/600/400/cola?lock=17");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Points", "TotalSpent" },
                values: new object[] { 0, 0m });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Points", "TotalSpent" },
                values: new object[] { 0, 0m });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId",
                table: "Orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteItems_ComboId",
                table: "FavoriteItems",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteItems_FastFoodId",
                table: "FavoriteItems",
                column: "FastFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteItems_UserId",
                table: "FavoriteItems",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_OrderId",
                table: "PointTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PointTransactions_UserId",
                table: "PointTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Branches_BranchId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "FavoriteItems");

            migrationBuilder.DropTable(
                name: "PointTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_BranchId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalSpent",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "/images/products/burger-cheese-double.jpg");

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "/images/products/pizza-seafood.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "/images/products/burger-cheese-double.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "/images/products/burger-bbq-bacon.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 3,
                column: "ImageUrl",
                value: "/images/products/pizza-seafood.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImageUrl",
                value: "/images/products/pizza-pepperoni.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 5,
                column: "ImageUrl",
                value: "/images/products/chicken-crispy.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 6,
                column: "ImageUrl",
                value: "/images/products/chicken-spicy-wings.jpg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 7,
                column: "ImageUrl",
                value: "/images/products/drink-coke.jpg");

            migrationBuilder.InsertData(
                table: "ModifierGroups",
                columns: new[] { "Id", "Description", "FastFoodId", "IsMultiple", "MaxOptions", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Chọn kích cỡ món ăn", 1, false, 1, "Size", 1 },
                    { 2, "Thêm nhân / phô mai", 1, true, 4, "Topping", 2 },
                    { 3, "Chọn kích cỡ", 3, false, 1, "Size", 1 },
                    { 4, "Chọn độ cay cho gà rán", 5, false, 1, "Độ cay", 1 }
                });

            migrationBuilder.InsertData(
                table: "ModifierOptions",
                columns: new[] { "Id", "IsAvailable", "IsDefault", "ModifierGroupId", "Name", "Price", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, true, 1, "Nhỏ", 0m, 1 },
                    { 2, true, false, 1, "Vừa", 10000m, 2 },
                    { 3, true, false, 1, "Lớn", 20000m, 3 },
                    { 4, true, false, 2, "Thêm phô mai", 8000m, 1 },
                    { 5, true, false, 2, "Thêm thịt bò", 15000m, 2 },
                    { 6, true, false, 2, "Thêm rau & xà lách", 3000m, 3 },
                    { 7, true, true, 3, "Vừa (9 inch)", 0m, 1 },
                    { 8, true, false, 3, "Lớn (12 inch)", 30000m, 2 },
                    { 9, true, false, 3, "Đại (15 inch)", 55000m, 3 },
                    { 10, true, true, 4, "Không cay", 0m, 1 },
                    { 11, true, false, 4, "Cay nhẹ", 0m, 2 },
                    { 12, true, false, 4, "Cay nồng", 5000m, 3 }
                });
        }
    }
}
