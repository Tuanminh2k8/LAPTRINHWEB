namespace Source.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Preparing = "Preparing";
        public const string ReadyForPickup = "ReadyForPickup";
        public const string DriverAssigned = "DriverAssigned";
        public const string PickedUp = "PickedUp";
        public const string Shipping = "Shipping";
        public const string Arriving = "Arriving";
        public const string Delivered = "Delivered";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string RefundPending = "RefundPending";
        public const string Refunded = "Refunded";

        public static readonly string[] All =
        {
            Pending, Confirmed, Preparing, ReadyForPickup, DriverAssigned,
            PickedUp, Shipping, Arriving, Delivered, Completed,
            Cancelled, RefundPending, Refunded
        };

        // Các trạng thái mà khách vẫn có thể tương tác (chưa kết thúc)
        public static readonly string[] Active = { Pending, Confirmed, Preparing, ReadyForPickup, DriverAssigned, PickedUp, Shipping, Arriving };

        /// <summary>Các trạng thái đang vận chuyển (có tài xế).</summary>
        public static readonly string[] InDelivery = { DriverAssigned, PickedUp, Shipping, Arriving };

        public static bool IsValid(string? status) =>
            !string.IsNullOrEmpty(status) && All.Contains(status);

        /// <summary>Kiểm tra xem chuyển đổi từ status hiện tại sang status mới có hợp lệ theo quy trình không.</summary>
        public static bool IsValidTransition(string currentStatus, string targetStatus)
        {
            var validTransitions = new Dictionary<string, string[]>
            {
                [Pending] = new[] { Confirmed, Cancelled },
                [Confirmed] = new[] { Preparing, Cancelled },
                [Preparing] = new[] { ReadyForPickup },
                [ReadyForPickup] = new[] { DriverAssigned },
                [DriverAssigned] = new[] { PickedUp },
                [PickedUp] = new[] { Shipping },
                [Shipping] = new[] { Arriving, Delivered },
                [Arriving] = new[] { Delivered },
                [Delivered] = new[] { Completed, RefundPending },
                [RefundPending] = new[] { Refunded },
                [Completed] = new[] { RefundPending },
                [Refunded] = new string[0],
                [Cancelled] = new string[0]
            };

            return validTransitions.ContainsKey(currentStatus)
                && validTransitions[currentStatus].Contains(targetStatus);
        }

        /// <summary>Nhãn tiếng Việt hiển thị thống nhất toàn hệ thống (khách + admin).</summary>
        public static string GetLabel(string status)
        {
            return status switch
            {
                Pending => "Chờ xác nhận",
                Confirmed => "Đã xác nhận",
                Preparing => "Đang chuẩn bị",
                ReadyForPickup => "Sẵn sàng bàn giao",
                DriverAssigned => "Đã có tài xế",
                PickedUp => "Tài xế đã lấy hàng",
                Shipping => "Đang giao",
                Arriving => "Sắp đến nơi",
                Delivered => "Đã giao",
                Completed => "Hoàn thành",
                Cancelled => "Đã hủy",
                RefundPending => "Chờ hoàn tiền",
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
                Confirmed => "bg-info text-dark",
                Preparing => "bg-info text-dark",
                ReadyForPickup => "bg-primary",
                DriverAssigned => "bg-primary",
                PickedUp => "bg-primary",
                Shipping => "bg-primary",
                Arriving => "bg-primary",
                Delivered => "bg-success",
                Completed => "bg-success",
                Cancelled => "bg-danger",
                RefundPending => "bg-secondary",
                Refunded => "bg-secondary",
                _ => "bg-secondary"
            };
        }

        /// <summary>Chuỗi trạng thái tiếp theo theo quy trình chuẩn.</summary>
        public static string? GetNext(string status)
        {
            return status switch
            {
                Pending => Confirmed,
                Confirmed => Preparing,
                Preparing => ReadyForPickup,
                ReadyForPickup => DriverAssigned,
                DriverAssigned => PickedUp,
                PickedUp => Shipping,
                Shipping => Arriving,
                Arriving => Delivered,
                Delivered => Completed,
                _ => null
            };
        }

        /// <summary>Icon Font Awesome cho từng trạng thái.</summary>
        public static string GetIcon(string status)
        {
            return status switch
            {
                Pending => "fa-clock",
                Confirmed => "fa-check",
                Preparing => "fa-fire-burner",
                ReadyForPickup => "fa-box-open",
                DriverAssigned => "fa-motorcycle",
                PickedUp => "fa-boxes-stacked",
                Shipping => "fa-truck-fast",
                Arriving => "fa-location-dot",
                Delivered => "fa-circle-check",
                Completed => "fa-flag-checkered",
                Cancelled => "fa-ban",
                RefundPending => "fa-rotate-left",
                Refunded => "fa-money-bill-transfer",
                _ => "fa-circle"
            };
        }
    }
}