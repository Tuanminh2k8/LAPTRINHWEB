using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    /// <summary>
    /// Sổ cái điểm thưởng (Loyalty / Membership) — ghi nhận mọi biến động điểm của khách hàng.
    /// Type: Earn (tích), Redeem (đổi), Adjust (điều chỉnh bởi admin), Expire (hết hạn).
    /// Points là giá trị có dấu: dương khi tích, âm khi đổi.
    /// </summary>
    public class PointTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Loại giao dịch")]
        public string Type { get; set; } = "Earn";

        [Display(Name = "Điểm")]
        public int Points { get; set; }

        [Display(Name = "Số dư sau giao dịch")]
        public int BalanceAfter { get; set; }

        [StringLength(250)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Thời gian")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
