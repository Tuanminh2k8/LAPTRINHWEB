using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Source.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Combos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GoogleId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FastFoods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FastFoods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FastFoods_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceiverAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ComboDetails",
                columns: table => new
                {
                    ComboId = table.Column<int>(type: "int", nullable: false),
                    FastFoodId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComboDetails", x => new { x.ComboId, x.FastFoodId });
                    table.ForeignKey(
                        name: "FK_ComboDetails_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ComboDetails_FastFoods_FastFoodId",
                        column: x => x.FastFoodId,
                        principalTable: "FastFoods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    FastFoodId = table.Column<int>(type: "int", nullable: true),
                    ComboId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderDetails_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderDetails_FastFoods_FastFoodId",
                        column: x => x.FastFoodId,
                        principalTable: "FastFoods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderDetails_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "Các loại bánh burger thơm ngon", "Burgers" },
                    { 2, "Pizza nóng hổi, nhiều phô mai", "Pizzas" },
                    { 3, "Gà chiên giòn rụm", "Gà Rán" },
                    { 4, "Nước ngọt, khoai tây chiên, kem", "Thức uống & Tráng miệng" }
                });

            migrationBuilder.InsertData(
                table: "Combos",
                columns: new[] { "Id", "Description", "ImageUrl", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "2 Burger Bò Phô Mai + 1 Khoai Tây Chiên + 2 Coca Cola. Tiết kiệm hơn!", "/images/combo_family.svg", "Combo Gia Đình", 150000m },
                    { 2, "1 Pizza Hải Sản + 1 Gà Rán Giòn Cay + 1 Khoai Tây Chiên + 2 Coca Cola. Cực vui cực đã!", "/images/combo_party.svg", "Combo Tiệc Tùng", 200000m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "Email", "FullName", "GoogleId", "PasswordHash", "PhoneNumber", "Role", "Username" },
                values: new object[,]
                {
                    { 1, "123 Đường Tô Ký, Quận 12, TP.HCM", "admin@fastfood.com", "Quản Trị Viên", null, "$2a$11$ezY8eus712l.J/TErYvnveHybjXijpr.j7gucKR7G0q3xlgK6WCc6", "0987654321", "Admin", "admin" },
                    { 2, "456 Đường Quang Trung, Gò Vấp, TP.HCM", "customer@fastfood.com", "Nguyễn Văn Khách", null, "$2a$11$YIt.Q8rHNv0BKrlePDKezedHKn7OjqQYdbTAS7EramaJSAVPn.R/6", "0912345678", "Customer", "customer" }
                });

            migrationBuilder.InsertData(
                table: "FastFoods",
                columns: new[] { "Id", "CategoryId", "Description", "ImageUrl", "Name", "Price", "Theme" },
                values: new object[,]
                {
                    { 1, 1, "Bánh burger kẹp thịt bò nướng thơm ngon cùng lớp phô mai béo ngậy và rau tươi.", "/images/burger_cheese.svg", "Burger Bò Phô Mai", 55000m, "Gia đình" },
                    { 2, 1, "Bánh burger kẹp thịt gà chiên giòn tan, sốt mayonnaise và xà lách ngon tuyệt.", "/images/burger_chicken.svg", "Burger Gà Giòn", 50000m, "Trẻ em" },
                    { 3, 2, "Pizza với mực, tôm, thanh cua tươi ngon cùng phô mai Mozzarella thượng hạng.", "/images/pizza_seafood.svg", "Pizza Hải Sản", 120000m, "Tiệc tùng" },
                    { 4, 2, "Pizza đầy ắp thịt nguội, xúc xích pepperoni, ớt chuông, nấm và phô mai.", "/images/pizza_mixed.svg", "Pizza Thập Cẩm", 110000m, "Gia đình" },
                    { 5, 3, "Một miếng gà rán giòn rụm, tẩm ướp gia vị cay nồng đậm đà.", "/images/chicken_spicy.svg", "Gà Rán Giòn Cay", 35000m, "Ăn vặt" },
                    { 6, 4, "Khoai tây chiên vàng giòn, rắc chút muối thơm ngon.", "/images/fries.svg", "Khoai Tây Chiên", 25000m, "Ăn vặt" },
                    { 7, 4, "Nước ngọt có ga Coca Cola mát lạnh.", "/images/coca.svg", "Coca Cola", 15000m, "Ăn uống" }
                });

            migrationBuilder.InsertData(
                table: "ComboDetails",
                columns: new[] { "ComboId", "FastFoodId", "Quantity" },
                values: new object[,]
                {
                    { 1, 1, 2 },
                    { 1, 6, 1 },
                    { 1, 7, 2 },
                    { 2, 3, 1 },
                    { 2, 5, 1 },
                    { 2, 6, 1 },
                    { 2, 7, 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComboDetails_FastFoodId",
                table: "ComboDetails",
                column: "FastFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_FastFoods_CategoryId",
                table: "FastFoods",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_ComboId",
                table: "OrderDetails",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_FastFoodId",
                table: "OrderDetails",
                column: "FastFoodId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderDetails_OrderId",
                table: "OrderDetails",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComboDetails");

            migrationBuilder.DropTable(
                name: "OrderDetails");

            migrationBuilder.DropTable(
                name: "Combos");

            migrationBuilder.DropTable(
                name: "FastFoods");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
