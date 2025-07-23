using eShopApi.Core.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class BannerCreateDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public IFormFile Image { get; set; }

        public string? LinkUrl { get; set; }

        public string? ButtonText { get; set; }

        [Required]
        public BannerPosition Position { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }
    }
    public class BannerUpdateDto
    {
        [Required]
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public IFormFile? Image { get; set; }

        public string? LinkUrl { get; set; }

        public string? ButtonText { get; set; }

        public BannerPosition Position { get; set; }

        public bool? IsActive { get; set; }

        public int? SortOrder { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
    public class BannerResponseDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public string ImageUrl { get; set; }

        public string? LinkUrl { get; set; }

        public string? ButtonText { get; set; }

        public BannerPosition Position { get; set; }

        public bool IsActive { get; set; }

        public int SortOrder { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
