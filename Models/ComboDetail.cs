using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Source.Models
{
    public class ComboDetail
    {
        [Required]
        public int ComboId { get; set; }

        [ForeignKey("ComboId")]
        public Combo? Combo { get; set; }

        [Required]
        public int FastFoodId { get; set; }

        [ForeignKey("FastFoodId")]
        public FastFood? FastFood { get; set; }

        [Required(ErrorMessage = "Số lượng không được để trống")]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        [Display(Name = "Số lượng")]
        public int Quantity { get; set; } = 1;
    }
}
