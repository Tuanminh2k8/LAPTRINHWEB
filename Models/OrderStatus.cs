namespace Source.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Chưa giao";
        public const string Delivering = "Đang giao";
        public const string Delivered = "Đã giao";

        public static readonly string[] All = { Pending, Delivering, Delivered };

        public static bool IsValid(string? status) =>
            !string.IsNullOrEmpty(status) && All.Contains(status);
    }
}
