using System.ComponentModel.DataAnnotations;

namespace Source.Models
{
    public class ReviewImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ReviewId { get; set; }

        public Review? Review { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int SortOrder { get; set; }
    }
}
