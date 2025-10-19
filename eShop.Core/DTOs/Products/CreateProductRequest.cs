using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs.Products
{
    public class CreateProductRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }
        [Required]
        public string? Description { get; set; }
        [Required]
        public string? ShortDescription { get; set; }
        [Required, MaxLength(100)]
        public string Sku { get; set; }
        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "Compare price must be greater than 0")]
        [Required]
        public decimal? ComparePrice { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        [Required]
        public int StockQuantity { get; set; }
        [Required]
        public bool TrackQuantity { get; set; } = true;
        [Required]
        public bool IsActive { get; set; } = true;
        [Required]
        public bool IsFeatured { get; set; } = false;
        [Range(0, double.MaxValue, ErrorMessage = "Weight cannot be negative")]
        public double? Weight { get; set; } = 0;
        [Required]
        public string? Dimensions { get; set; }
        [Required]
        public string? Tags { get; set; }
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public ICollection<CreateProductImageRequest> ProductImages { get; set; }
    }
}
