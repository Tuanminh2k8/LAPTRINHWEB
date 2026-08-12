using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class OrderDetailModifier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Chi tiết đơn")]
        public int OrderDetailId { get; set; }

        [Display(Name = "Chi tiết đơn")]
        public OrderDetail? OrderDetail { get; set; }

        [Required]
        [Display(Name = "Tùy chọn")]
        public int ModifierOptionId { get; set; }

        [Display(Name = "Tùy chọn")]
        public ModifierOption? ModifierOption { get; set; }

        [StringLength(100)]
        [Display(Name = "Tên tùy chọn (snapshot)")]
        public string OptionName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá tăng thêm (snapshot)")]
        public decimal OptionPrice { get; set; } = 0;
    }
}