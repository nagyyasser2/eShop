using System.ComponentModel.DataAnnotations;
using eShop.Core.DTOs.Category;

namespace eShop.Core.DTOs.Products
{
    public class ProductDto
    {
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        [Required, MaxLength(100)]
        public string Sku { get; set; }
        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Compare price must be greater than 0")]
        public decimal? ComparePrice { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }
        public bool TrackQuantity { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        [Range(0, double.MaxValue, ErrorMessage = "Weight cannot be negative")]
        public double Weight { get; set; } = 0;
        public string? Dimensions { get; set; }
        public string? Tags { get; set; }
        public int CategoryId { get; set; }
        public List<ProductImageDto> ProductImages { get; set; }
        public CategoryDto? Category { get; set; }
    }
}
