namespace Source.Models
{
    public class CartItemModifier
    {
        public int OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public decimal OptionPrice { get; set; }
    }

    public class CartItem
    {
        public int? FastFoodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        // Giá chưa gồm tùy chọn; các CartItemModifier cộng thêm
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsCombo { get; set; }
        public int? ComboId { get; set; }
        public List<CartItemModifier> Modifiers { get; set; } = new();

        public decimal UnitPrice => Price + Modifiers.Sum(m => m.OptionPrice) * (IsCombo ? 0 : 1);
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}
