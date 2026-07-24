using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Source.Migrations
{
    /// <inheritdoc />
    public partial class AddComboSaleProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnSale",
                table: "Combos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "Combos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ImageUrl", "IsOnSale", "OriginalPrice" },
                values: new object[] { "/images/products/burger-cheese-double.jpg", false, 0m });

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ImageUrl", "IsOnSale", "OriginalPrice" },
                values: new object[] { "/images/products/pizza-seafood.jpg", false, 0m });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnSale",
                table: "Combos");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "Combos");

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "/images/combo_family.svg");

            migrationBuilder.UpdateData(
                table: "Combos",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "/images/combo_party.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 1,
                column: "ImageUrl",
                value: "/images/burger_cheese.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 2,
                column: "ImageUrl",
                value: "/images/burger_chicken.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 3,
                column: "ImageUrl",
                value: "/images/pizza_seafood.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 4,
                column: "ImageUrl",
                value: "/images/pizza_mixed.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 5,
                column: "ImageUrl",
                value: "/images/chicken_spicy.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 6,
                column: "ImageUrl",
                value: "/images/fries.svg");

            migrationBuilder.UpdateData(
                table: "FastFoods",
                keyColumn: "Id",
                keyValue: 7,
                column: "ImageUrl",
                value: "/images/coca.svg");
        }
    }
}
