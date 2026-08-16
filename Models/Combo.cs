using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class Combo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên combo không được để trống")]
        [StringLength(100, ErrorMessage = "Tên combo không được quá 100 ký tự")]
        [Display(Name = "Tên combo")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá combo không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá combo phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá combo")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả combo không được để trống")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string ImageUrl { get; set; } = "/images/default_combo.svg";

        [Display(Name = "Đang giảm giá")]
        public bool IsOnSale { get; set; } = false;

        [Display(Name = "Giá gốc")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OriginalPrice { get; set; } = 0;

        [Display(Name = "Mã SKU")]
        [StringLength(50)]
        public string? Sku { get; set; }

        // Navigation property for foods in this combo
        public ICollection<ComboDetail> ComboDetails { get; set; } = new List<ComboDetail>();
    }
}
