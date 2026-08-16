using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Source.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerToFastFood : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_FastFoods_FastFoodId",
                table: "OrderDetails");

            migrationBuilder.AddColumn<int>(
                name: "SellerId",
                table: "FastFoods",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 1,
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 2,
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 3,
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 4,
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 5,
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 6,
                column: "SellerId",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 7,
                column: "SellerId",
                value: null);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "Email", "FullName", "GoogleId", "PasswordHash", "PhoneNumber", "Points", "Role", "TotalSpent", "Username" },
                values: new object[] { 3, "789 Đường Lê Lợi, Quận 1, TP.HCM", "seller@fastfood.com", "Người Bán Hàng Shopee", null, "$2a$11$YIt.Q8rHNv0BKrlePDKezedHKn7OjqQYdbTAS7EramaJSAVPn.R/6", "0909090909", 0, "Seller", 0m, "seller" });

            migrationBuilder.CreateIndex(
                name: "IX_FastFoods_SellerId",
                table: "FastFoods",
                column: "SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_FastFoods_Users_SellerId",
                table: "FastFoods",
                column: "SellerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_FastFoods_FastFoodId",
                table: "OrderDetails",
                column: "FastFoodId",
                principalTable: "FastFoods",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FastFoods_Users_SellerId",
                table: "FastFoods");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderDetails_FastFoods_FastFoodId",
                table: "OrderDetails");

            migrationBuilder.DropIndex(
                name: "IX_FastFoods_SellerId",
                table: "FastFoods");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "FastFoods");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_Combos_ComboId",
                table: "OrderDetails",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderDetails_FastFoods_FastFoodId",
                table: "OrderDetails",
                column: "FastFoodId",
                principalTable: "FastFoods",
                principalColumn: "Id");
        }
    }
}
