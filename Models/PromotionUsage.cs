using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class PromotionUsage
    {
        [Key]
        public int Id { get; set; }

        public int PromotionId { get; set; }

        [ForeignKey("PromotionId")]
        public PromoCode Promotion { get; set; } = null!;

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalOrderAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalOrderAmount { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;

        [Required, StringLength(20)]
        public string Status { get; set; } = nameof(PromotionUsageStatus.Used);

        [StringLength(50)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }
    }
}
