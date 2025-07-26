using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        [Required, MaxLength(100)]
        public string SKU { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ComparePrice { get; set; }
        public int StockQuantity { get; set; }
        public bool TrackQuantity { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public double Weight { get; set; } = 0;
        public string? Dimensions { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int? CategoryId { get; set; }

        // Navigation Properties
        public virtual Category? Category { get; set; }
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
        public virtual ICollection<Variant> Variants { get; set; } = new List<Variant>();
    }
}
