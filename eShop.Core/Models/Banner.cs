using eShopApi.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Banner
    {
        public int Id { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; }
        public string? Description { get; set; }
        [Required]
        public string ImageUrl { get; set; }
        public string? LinkUrl { get; set; }
        public string? ButtonText { get; set; }
        public BannerPosition Position { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
