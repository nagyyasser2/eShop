
namespace eShop.Core.Models
{
    public class WishListItem
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public int Priority { get; set; } = 0; // User can prioritize items

        // Foreign Keys
        public int WishListId { get; set; }
        public int ProductId { get; set; }

        // Navigation Properties
        public virtual WishList WishList { get; set; }
        public virtual Product Product { get; set; }
    }
}
