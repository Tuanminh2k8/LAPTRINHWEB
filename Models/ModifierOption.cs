using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class ModifierOption
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên tùy chọn không được để trống")]
        [StringLength(100, ErrorMessage = "Tên tùy chọn không được quá 100 ký tự")]
        [Display(Name = "Tên tùy chọn")]
        public string Name { get; set; } = string.Empty; // e.g. Nhỏ, Vừa, Lớn / Thêm phô mai / Không cay

        [Display(Name = "Giá tăng thêm")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá tăng thêm phải lớn hơn hoặc bằng 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } = 0;

        [Display(Name = "Chọn mặc định")]
        public bool IsDefault { get; set; } = false;

        [Display(Name = "Khả dụng")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Sắp xếp")]
        public int SortOrder { get; set; } = 0;

        [Required]
        [Display(Name = "Nhóm tùy chọn")]
        public int ModifierGroupId { get; set; }

        [Display(Name = "Nhóm tùy chọn")]
        public ModifierGroup? ModifierGroup { get; set; }
    }
}