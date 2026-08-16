using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    /// <summary>Sự kiện theo dõi đơn hàng (timeline) — mỗi lần chuyển trạng thái ghi 1 dòng lịch sử.</summary>
    public class OrderTrackingEvent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Đơn hàng")]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        [Display(Name = "Đơn hàng")]
        public Order? Order { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [StringLength(50)]
        [Display(Name = "Người thực hiện")]
        public string? Actor { get; set; } // System | Seller | Driver | Admin | Customer

        [Display(Name = "Thời điểm")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}