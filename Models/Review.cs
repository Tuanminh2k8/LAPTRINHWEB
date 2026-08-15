using System.ComponentModel.DataAnnotations;

namespace Source.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Đơn hàng")]
        public int OrderId { get; set; }

        [Display(Name = "Đơn hàng")]
        public Order? Order { get; set; }

        [Required]
        [Display(Name = "Món ăn")]
        public int FastFoodId { get; set; }

        [Display(Name = "Món ăn")]
        public FastFood? FastFood { get; set; }

        [Required]
        [Display(Name = "Người đánh giá")]
        public int UserId { get; set; }

        [Display(Name = "Người đánh giá")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao")]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        [Display(Name = "Số sao")]
        public int Rating { get; set; } = 5;

        [StringLength(1000, ErrorMessage = "Nhận xét không được quá 1000 ký tự")]
        [Display(Name = "Nhận xét")]
        public string? Comment { get; set; }

        [Display(Name = "Ngày đánh giá")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Xác nhận đơn đã giao thành công thì được đánh giá
        [Display(Name = "Đã kiểm duyệt")]
        public bool IsApproved { get; set; } = true;

        public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
    }
}
