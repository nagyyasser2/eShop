using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class WishList
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; } = "My Wishlist";
        public string? Description { get; set; }
        public bool IsPublic { get; set; } = false;
        public bool IsDefault { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public string UserId { get; set; }

        // Navigation Properties
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<WishListItem> WishListItems { get; set; } = new List<WishListItem>();
    }
}
