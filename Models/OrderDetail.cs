using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        public int? FastFoodId { get; set; }

        [ForeignKey("FastFoodId")]
        public FastFood? FastFood { get; set; }

        public int? ComboId { get; set; }

        [ForeignKey("ComboId")]
        public Combo? Combo { get; set; }

        [Required]
        [Range(1, 100)]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Đơn giá")]
        public decimal Price { get; set; }

        // Snapshot tên món để hiển thị lại sau khi xóa sản phẩm
        [StringLength(100)]
        public string? FastFoodName { get; set; }

        public ICollection<OrderDetailModifier> Modifiers { get; set; } = new List<OrderDetailModifier>();
    }
}
