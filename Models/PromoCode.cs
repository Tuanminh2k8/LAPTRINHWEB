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
        [Display(Name = "Tên chương trình")]
        public string? Name { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        /// <summary>"Percent" = giảm %, "Amount" = giảm tiền cố định (VNĐ)</summary>
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

        [Display(Name = "Giới hạn mỗi user")]
        public int MaxUsagePerUser { get; set; } = 0; // 0 = không giới hạn

        [Display(Name = "Bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Hết hạn")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Kích hoạt (legacy)")]
        public bool IsActive { get; set; } = true;

        // ---- New promotion system fields ----
        [Required, StringLength(20)]
        public string Status { get; set; } = nameof(PromotionStatus.Active);

        public bool IsPublished { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public bool IsEarlyPublished { get; set; } = false;
        public bool IsVisibleEarly { get; set; } = false;   // hiển thị trước StartDate
        public bool IsUsableEarly { get; set; } = false;    // cho phép dùng trước StartDate
        public bool IsDeleted { get; set; } = false;        // soft delete

        [Required, StringLength(20)]
        public string OwnerRole { get; set; } = nameof(PromotionOwnerRole.Admin);

        public int? SellerId { get; set; }

        public int Priority { get; set; } = 0;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(500)]
        public string? BannerUrl { get; set; }

        /// <summary>Chủ đề trang trí trên trang chủ: None | Halloween | Winter | NewYear | Valentine | Summer</summary>
        [StringLength(30)]
        [Display(Name = "Chủ đề trang trí")]
        public string? Theme { get; set; }

        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        // Navigation
        public User? Seller { get; set; }
        public ICollection<PromotionUsage> Usages { get; set; } = new List<PromotionUsage>();
    }
}
