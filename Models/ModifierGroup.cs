using System.ComponentModel.DataAnnotations;

namespace Source.Models
{
    public class ModifierGroup
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhóm tùy chọn không được để trống")]
        [StringLength(100, ErrorMessage = "Tên nhóm tùy chọn không được quá 100 ký tự")]
        [Display(Name = "Tên nhóm tùy chọn")]
        public string Name { get; set; } = string.Empty; // e.g. Size, Topping, Độ cay

        [Display(Name = "Hiển thị mô tả")]
        [StringLength(200)]
        public string? Description { get; set; }

        // false = chỉ chọn 1 (radio), true = chọn nhiều (checkbox)
        [Display(Name = "Cho phép chọn nhiều")]
        public bool IsMultiple { get; set; } = false;

        // Số tùy chọn tối đa được phép chọn (chỉ áp dụng khi IsMultiple = true)
        [Range(1, 100, ErrorMessage = "Số tùy chọn tối đa phải từ 1 đến 100")]
        [Display(Name = "Số tùy chọn tối đa")]
        public int MaxOptions { get; set; } = 1;

        // Số tùy chọn tối thiểu phải chọn (0 = không bắt buộc, 1 = bắt buộc chọn ít nhất 1)
        [Range(0, 100, ErrorMessage = "Số tùy chọn tối thiểu phải từ 0 đến 100")]
        [Display(Name = "Số tùy chọn tối thiểu")]
        public int MinOptions { get; set; } = 0;

        [Display(Name = "Sắp xếp")]
        public int SortOrder { get; set; } = 0;

        [Required]
        [Display(Name = "Món ăn")]
        public int FastFoodId { get; set; }

        [Display(Name = "Món ăn")]
        public FastFood? FastFood { get; set; }

        public ICollection<ModifierOption> Options { get; set; } = new List<ModifierOption>();
    }
}