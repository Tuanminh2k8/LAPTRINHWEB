using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Display(Name = "Ngày đặt hàng")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Display(Name = "Tổng tiền")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Chưa giao"; // Chưa giao, Đang giao, Đã giao

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được quá 100 ký tự")]
        [Display(Name = "Người nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại nhận hàng không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ")]
        [Display(Name = "Số điện thoại nhận")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống")]
        [StringLength(200, ErrorMessage = "Địa chỉ nhận hàng không được quá 200 ký tự")]
        [Display(Name = "Địa chỉ nhận")]
        public string ReceiverAddress { get; set; } = string.Empty;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
