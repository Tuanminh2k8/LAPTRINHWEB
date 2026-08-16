using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Source.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PromoCode",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SellerName",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OptionQuantity",
                table: "OrderDetailModifiers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "FastFoods",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Combos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 1,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 2,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 1,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 2,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 3,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 4,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 5,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 6,
                column: "Sku",
                value: null);

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 7,
                column: "Sku",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromoCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Tax",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerName",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "OptionQuantity",
                table: "OrderDetailModifiers");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "FastFoods");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Combos");
        }
    }
}
