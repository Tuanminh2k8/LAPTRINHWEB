using System.ComponentModel.DataAnnotations;

namespace Source.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chủng loại không được để trống")]
        [StringLength(100, ErrorMessage = "Tên chủng loại không được quá 100 ký tự")]
        [Display(Name = "Tên chủng loại")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250, ErrorMessage = "Mô tả không được quá 250 ký tự")]
        [Display(Name = "Mô tả")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Emoji / icon hiển thị bên cạnh tên món, lấy từ bộ ảnh local.</summary>
        [Display(Name = "Biểu tượng")]
        public string Icon { get; set; } = "🍔";

        // Navigation property
        public ICollection<FastFood> FastFoods { get; set; } = new List<FastFood>();
    }
}
