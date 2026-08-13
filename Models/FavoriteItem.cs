using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    /// <summary>
    /// Món ăn / combo yêu thích của khách hàng (tính năng Favorites theo mô hình KFC "nhớ món đã đặt").
    /// Mỗi cặp (UserId, FastFoodId/ComboId) chỉ lưu một lần.
    /// </summary>
    public class FavoriteItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public int? FastFoodId { get; set; }

        [ForeignKey("FastFoodId")]
        public FastFood? FastFood { get; set; }

        public int? ComboId { get; set; }

        [ForeignKey("ComboId")]
        public Combo? Combo { get; set; }

        [Display(Name = "Ngày thêm")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
