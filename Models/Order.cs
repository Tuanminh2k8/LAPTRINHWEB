using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // Nullable: khách đặt hàng không cần tài khoản (guest checkout)
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        /// <summary>Delivery = Giao tận nơi, Pickup = Tự đến lấy.</summary>
        [Required]
        [Display(Name = "Loại đơn")]
        public string OrderType { get; set; } = "Delivery";

        [Display(Name = "Giờ hẹn đến lấy")]
        public DateTime? PickupTime { get; set; }

        /// <summary>Chi nhánh nhận hàng (bắt buộc khi OrderType = Pickup).</summary>
        [Display(Name = "Chi nhánh")]
        public int? BranchId { get; set; }

        [ForeignKey("BranchId")]
        [Display(Name = "Chi nhánh")]
        public Branch? Branch { get; set; }

        /// <summary>Mã tham chiếu thanh toán (sandbox: Guid tự sinh).</summary>
        [StringLength(100)]
        [Display(Name = "Mã thanh toán")]
        public string? PaymentReference { get; set; }

        /// <summary>Thời điểm thanh toán thành công.</summary>
        [Display(Name = "Ngày thanh toán")]
        public DateTime? PaidAt { get; set; }

        /// <summary>Chờ thanh toán / Đã thanh toán / Hoàn tiền (cho đơn chuyển khoản).</summary>
        [Required]
        [Display(Name = "Trạng thái thanh toán")]
        public string PaymentStatus { get; set; } = "Unpaid";

        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Tổng tiền")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = OrderStatus.Pending;

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được quá 100 ký tự")]
        [Display(Name = "Người nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại nhận hàng không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ")]
        [Display(Name = "Số điện thoại nhận")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống")]
        [StringLength(200, ErrorMessage = "Địa chỉ nhận hàng không được quá 200 ký tự")]
        [Display(Name = "Địa chỉ nhận")]
        public string ReceiverAddress { get; set; } = string.Empty;

        [Display(Name = "Phương thức thanh toán")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "COD";

        [Display(Name = "Phí vận chuyển")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; } = 0;

        [Display(Name = "Giảm giá")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; } = 0;

        [Display(Name = "Ghi chú")]
        [StringLength(500)]
        public string? Note { get; set; }

        [Display(Name = "Lý do hủy")]
        [StringLength(500)]
        public string? CancelReason { get; set; }

        [Display(Name = "Đã xóa")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Cập nhật lần cuối")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
