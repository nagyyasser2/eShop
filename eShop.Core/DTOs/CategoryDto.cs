using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; }

        // Parent category info
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }

        // Child categories (for hierarchical display)
        public List<CategoryDto> ChildCategories { get; set; } = new List<CategoryDto>();

        // Product count for display purposes
        public int ProductCount { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public int? ParentCategoryId { get; set; }

        // For file uploads
        public List<IFormFile>? ImageFiles { get; set; }
    }

    public class UpdateCategoryDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }

        public int? ParentCategoryId { get; set; }

        // For file uploads
        public List<IFormFile>? ImageFiles { get; set; }

        // For removing existing images
        public List<string>? ImageUrlsToRemove { get; set; }
    }

    // Simplified DTO for dropdown lists and quick references
    public class CategorySummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }

    // For hierarchical tree view
    public class CategoryTreeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new List<CategoryTreeDto>();
    }
}