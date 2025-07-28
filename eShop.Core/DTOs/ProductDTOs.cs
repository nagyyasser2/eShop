using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class CreateProductDto
    {
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }
        public string? ShortDescription { get; set; }

        [Required, MaxLength(100)]
        public string SKU { get; set; }

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
        public int? CategoryId { get; set; }

        public IList<IFormFile>? Images { get; set; }
        public IList<CreateVariantDTO>? Variants { get; set; }
    }

    // DTO for updating products via API (includes file uploads)
    public class UpdateProductDto
    {
        [Required]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        public string? Description { get; set; }
        public string? ShortDescription { get; set; }

        [Required, MaxLength(100)]
        public string SKU { get; set; }

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Compare price must be greater than 0")]
        public decimal? ComparePrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        public bool TrackQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Weight cannot be negative")]
        public double Weight { get; set; }

        public string? Dimensions { get; set; }
        public string? Tags { get; set; }
        public int? CategoryId { get; set; }
        public IList<UpdateVariantDTO>? Variants { get; set; }
        
        // File uploads for new images
        public IList<IFormFile>? NewImages { get; set; }

        // Image IDs to delete
        public IList<int>? ImageIdsToDelete { get; set; }
    }

    // DTO for updating stock quantity
    public class UpdateStockDto
    {
        [Required, Range(0, int.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public int Quantity { get; set; }
    }

    // DTO for retrieving product details
    public class ProductDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public decimal? ComparePrice { get; set; }
        public int StockQuantity { get; set; }
        public bool TrackQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public double Weight { get; set; }
        public string? Dimensions { get; set; }
        public string? Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CategoryId { get; set; }
        public CategoryDto? Category { get; set; }
        public IList<ImageDTO> Images { get; set; } = new List<ImageDTO>();
        public IList<VariantDTO> Variants { get; set; } = new List<VariantDTO>();
    }
}