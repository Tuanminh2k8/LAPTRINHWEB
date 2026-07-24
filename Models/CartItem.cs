namespace Source.Models
{
    public class CartItem
    {
        public int? FastFoodId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsCombo { get; set; }
        public int? ComboId { get; set; }

        public decimal TotalPrice => Price * Quantity;
    }
}
