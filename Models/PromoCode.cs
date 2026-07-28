using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class PromoCode
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(30)]
        [Display(Name = "Mã giảm giá")]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        /// <summary>"Percent" = giảm %, "Amount" = giảm số tiền cố định (VNĐ)</summary>
        [Required, StringLength(10)]
        public string DiscountType { get; set; } = "Percent";

        [Display(Name = "Giá trị giảm")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Đơn tối thiểu")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmount { get; set; } = 0;

        [Display(Name = "Giảm tối đa")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxDiscountAmount { get; set; } = 0; // 0 = không giới hạn

        [Display(Name = "Số lượt tối đa")]
        public int MaxUsage { get; set; } = 0; // 0 = không giới hạn

        [Display(Name = "Đã dùng")]
        public int UsedCount { get; set; } = 0;

        [Display(Name = "Bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Hết hạn")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;
    }
}
