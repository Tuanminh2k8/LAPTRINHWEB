using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class FoodVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Món ăn")]
        public int FastFoodId { get; set; }

        [ForeignKey("FastFoodId")]
        [Display(Name = "Món ăn")]
        public FastFood? FastFood { get; set; }

        [Required(ErrorMessage = "Tên phân loại không được để trống")]
        [StringLength(100, ErrorMessage = "Tên phân loại không được quá 100 ký tự")]
        [Display(Name = "Tên phân loại")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kích thước không được để trống")]
        [StringLength(50, ErrorMessage = "Kích thước không được quá 50 ký tự")]
        [Display(Name = "Kích thước (Size)")]
        public string Size { get; set; } = string.Empty; // e.g. S, M, L, XL

        [Required(ErrorMessage = "Màu sắc không được để trống")]
        [StringLength(50, ErrorMessage = "Màu sắc không được quá 50 ký tự")]
        [Display(Name = "Màu sắc")]
        public string Color { get; set; } = string.Empty; // e.g. Đỏ, Xanh, Đen

        [Display(Name = "Giá bán")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Display(Name = "Giá gốc (để tính giảm giá)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OriginalPrice { get; set; }

        [Display(Name = "Mã SKU")]
        [StringLength(50)]
        public string? Sku { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho phải lớn hơn hoặc bằng 0")]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "Còn hàng")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Mặc định")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Sắp xếp")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Computed property for display
        [NotMapped]
        public string DisplayName => $"{Size} - {Color}";

        [NotMapped]
        public decimal DiscountPercent => OriginalPrice.HasValue && OriginalPrice.Value > Price 
            ? Math.Round((OriginalPrice.Value - Price) / OriginalPrice.Value * 100, 0) 
            : 0;
    }
}