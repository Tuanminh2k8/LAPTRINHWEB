using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    /// <summary>
    /// Chi nhánh cửa hàng (phục vụ nhận tại chỗ / pickup theo mô hình KFC Collection Point).
    /// </summary>
    public class Branch
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên chi nhánh không được để trống")]
        [StringLength(150, ErrorMessage = "Tên chi nhánh không được quá 150 ký tự")]
        [Display(Name = "Tên chi nhánh")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [StringLength(250, ErrorMessage = "Địa chỉ không được quá 250 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; } = string.Empty;

        [StringLength(20)]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "Quận / Huyện")]
        public string? District { get; set; }

        [Display(Name = "Giờ mở cửa")]
        public TimeSpan OpenTime { get; set; } = new TimeSpan(7, 0, 0);

        [Display(Name = "Giờ đóng cửa")]
        public TimeSpan CloseTime { get; set; } = new TimeSpan(22, 0, 0);

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Thứ tự hiển thị")]
        public int SortOrder { get; set; } = 0;

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
