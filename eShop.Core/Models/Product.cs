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
        public int? MinStockLevel { get; set; }
        public bool TrackQuantity { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public double Weight { get; set; } = 0;
        public string? Dimensions { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }
        public int? SubCategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? DiscountId { get; set; }

        // Navigation Properties
        public virtual Discount? Discount { get; set; }
        public virtual Category? Category { get; set; }
        public virtual Brand? Brand { get; set; }
        public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();
        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<WishList> WishLists { get; set; } = new List<WishList>();
    }
}
