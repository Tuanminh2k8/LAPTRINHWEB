namespace Source.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Preparing = "Preparing";
        public const string Shipping = "Shipping";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
        public const string Refunded = "Refunded";

        public static readonly string[] All = { Pending, Preparing, Shipping, Delivered, Cancelled, Refunded };

        public static bool IsValid(string? status) =>
            !string.IsNullOrEmpty(status) && All.Contains(status);
    }
}
