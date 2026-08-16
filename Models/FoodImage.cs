using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class FoodImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Món ăn")]
        public int FastFoodId { get; set; }

        [ForeignKey("FastFoodId")]
        [Display(Name = "Món ăn")]
        public FastFood? FastFood { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh không được để trống")]
        [StringLength(500, ErrorMessage = "Đường dẫn ảnh không được quá 500 ký tự")]
        [Display(Name = "Đường dẫn ảnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [Display(Name = "Thứ tự")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Ảnh chính")]
        public bool IsPrimary { get; set; } = false;

        [StringLength(200)]
        [Display(Name = "Chú thích ảnh")]
        public string? AltText { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}