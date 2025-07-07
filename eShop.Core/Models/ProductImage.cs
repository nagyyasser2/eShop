using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        [Required]
        public string ImageUrl { get; set; }
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int ProductId { get; set; }
        public int ImageId { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual Image Image { get; set; }
    }

}
