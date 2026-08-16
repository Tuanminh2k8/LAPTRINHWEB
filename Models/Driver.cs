using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    /// <summary>Tài xế giao hàng — dữ liệu thật, không fake. Location chỉ lưu khi driver gửi từ trình duyệt.</summary>
    public class Driver
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Tài khoản")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [Display(Name = "Tài khoản")]
        public User? User { get; set; }

        [Required(ErrorMessage = "Họ tên tài xế không được để trống")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Phương tiện")]
        public string? VehicleType { get; set; }

        [StringLength(20)]
        [Display(Name = "Biển số")]
        public string? LicensePlate { get; set; }

        [Display(Name = "Đánh giá")]
        [Range(0, 5)]
        public double Rating { get; set; } = 5.0;

        [Display(Name = "Tổng lượt giao")]
        public int TotalDeliveries { get; set; } = 0;

        [Display(Name = "Đang online")]
        public bool IsOnline { get; set; } = false;

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        // Vị trí hiện tại — chỉ lưu khi driver gửi qua browser geolocation (không tự sinh giả)
        [Display(Name = "Vĩ độ")]
        public double? CurrentLat { get; set; }

        [Display(Name = "Kinh độ")]
        public double? CurrentLng { get; set; }

        [Display(Name = "Cập nhật vị trí lúc")]
        public DateTime? LastLocationAt { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}