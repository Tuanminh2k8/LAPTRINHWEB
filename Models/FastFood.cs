using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class FastFood
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên món ăn không được để trống")]
        [StringLength(100, ErrorMessage = "Tên món ăn không được quá 100 ký tự")]
        [Display(Name = "Tên món ăn")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá tiền không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tiền phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Giá tiền")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Mô tả món ăn không được để trống")]
        [Display(Name = "Thông tin món ăn")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string ImageUrl { get; set; } = "/images/default_food.jpg";

        [Required(ErrorMessage = "Vui lòng chọn chủng loại")]
        [Display(Name = "Chủng loại")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        [Display(Name = "Chủng loại")]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Chủ đề không được để trống")]
        [StringLength(100, ErrorMessage = "Chủ đề không được quá 100 ký tự")]
        [Display(Name = "Chủ đề")]
        public string Theme { get; set; } = string.Empty; // e.g. Ăn sáng, Tiệc tùng, Gia đình, Ăn vặt

        [Display(Name = "Đã bán")]
        public int SoldCount { get; set; } = 0;

        [Display(Name = "Còn hàng")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Bán chạy")]
        public bool IsBestSeller { get; set; } = false;

        // Navigation properties
        public ICollection<ComboDetail> ComboDetails { get; set; } = new List<ComboDetail>();
        public ICollection<ModifierGroup> ModifierGroups { get; set; } = new List<ModifierGroup>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
