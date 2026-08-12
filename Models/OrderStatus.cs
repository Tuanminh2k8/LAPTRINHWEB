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

        // Các trạng thái mà khách vẫn có thể tương tác (chưa kết thúc)
        public static readonly string[] Active = { Pending, Preparing, Shipping };

        public static bool IsValid(string? status) =>
            !string.IsNullOrEmpty(status) && All.Contains(status);

        /// <summary>Nhãn tiếng Việt hiển thị thống nhất toàn hệ thống (khách + admin).</summary>
        public static string GetLabel(string status)
        {
            return status switch
            {
                Pending => "Chờ xác nhận",
                Preparing => "Đang chuẩn bị",
                Shipping => "Đang giao",
                Delivered => "Đã giao",
                Cancelled => "Đã hủy",
                Refunded => "Hoàn tiền",
                _ => status
            };
        }

        /// <summary>Class bootstrap badge tương ứng, dùng đồng bộ trong mọi view.</summary>
        public static string GetBadgeClass(string status)
        {
            return status switch
            {
                Pending => "bg-warning text-dark",
                Preparing => "bg-info text-dark",
                Shipping => "bg-primary",
                Delivered => "bg-success",
                Cancelled => "bg-danger",
                Refunded => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        /// <summary>Chuỗi trạng thái tiếp theo theo quy trình chuẩn (Pending → Preparing → Shipping → Delivered).</summary>
        public static string? GetNext(string status)
        {
            return status switch
            {
                Pending => Preparing,
                Preparing => Shipping,
                Shipping => Delivered,
                _ => null
            };
        }
    }
}
