using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Source.Migrations
{
    /// <inheritdoc />
    public partial class AddModifiersReviewsAndOrderTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "OrderType",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PaymentStatus",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PickupTime",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FastFoodName",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "FastFoods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsBestSeller",
                table: "FastFoods",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SoldCount",
                table: "FastFoods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ModifierGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsMultiple = table.Column<bool>(type: "bit", nullable: false),
                    MaxOptions = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    FastFoodId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModifierGroups_FastFoods_FastFoodId",
                        column: x => x.FastFoodId,
                        principalTable: "FastFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FastFoodId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_FastFoods_FastFoodId",
                        column: x => x.FastFoodId,
                        principalTable: "FastFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModifierOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ModifierGroupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModifierOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModifierOptions_ModifierGroups_ModifierGroupId",
                        column: x => x.ModifierGroupId,
                        principalTable: "ModifierGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetailModifiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderDetailId = table.Column<int>(type: "int", nullable: false),
                    ModifierOptionId = table.Column<int>(type: "int", nullable: false),
                    OptionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OptionPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetailModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetailModifiers_ModifierOptions_ModifierOptionId",
                        column: x => x.ModifierOptionId,
                        principalTable: "ModifierOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderDetailModifiers_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "Icon",
                value: "🍔");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "Icon",
                value: "🍕");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "Icon",
                value: "🍗");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "Icon",
                value: "🥤");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, true, 142 });

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, true, 98 });

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, false, 67 });

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, true, 74 });

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, true, 189 });

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, true, 210 });

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "IsAvailable", "IsBestSeller", "SoldCount" },
                values: new object[] { true, true, 320 });

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

            migrationBuilder.CreateIndex(
                name: "IX_ModifierGroups_FastFoodId",
                table: "ModifierGroups",
                column: "FastFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_ModifierOptions_ModifierGroupId",
                table: "ModifierOptions",
                column: "ModifierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailModifiers_ModifierOptionId",
                table: "OrderDetailModifiers",
                column: "ModifierOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetailModifiers_OrderDetailId",
                table: "OrderDetailModifiers",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_FastFoodId",
                table: "Reviews",
                column: "FastFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_OrderId",
                table: "Reviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OrderDetailModifiers");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "ModifierOptions");

            migrationBuilder.DropTable(
                name: "ModifierGroups");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PickupTime",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FastFoodName",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "FastFoods");

            migrationBuilder.DropColumn(
                name: "IsBestSeller",
                table: "FastFoods");

            migrationBuilder.DropColumn(
                name: "SoldCount",
                table: "FastFoods");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Categories");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Users_UserId",
                table: "Orders",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
