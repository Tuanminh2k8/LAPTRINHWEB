namespace Source.Models
{
    public class CartItemModifier
    {
        public int OptionId { get; set; }
        public string OptionName { get; set; } = string.Empty;
        public decimal OptionPrice { get; set; }
        public int OptionQuantity { get; set; } = 1;
    }

    public class CartItem
    {
        public int? FastFoodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string? Sku { get; set; }

        // Phân loại (FoodVariant) đã chọn — null nếu món không có phân loại
        public int? VariantId { get; set; }
        public string? VariantName { get; set; }
        public decimal? VariantPrice { get; set; }

        // Giá cơ sở (chưa gồm tùy chọn); CartItemModifier cộng thêm
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsCombo { get; set; }
        public int? ComboId { get; set; }
        public List<CartItemModifier> Modifiers { get; set; } = new();

        // Giá đơn vị = giá (variant nếu có, ngược lại giá gốc) + modifier
        public decimal UnitPrice =>
            (VariantPrice ?? Price) + Modifiers.Sum(m => m.OptionPrice * m.OptionQuantity) * (IsCombo ? 0 : 1);
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}