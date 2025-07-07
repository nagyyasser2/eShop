namespace eShop.Core.Models
{
    public class WishList
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public string UserId { get; set; }
        public int ProductId { get; set; }

        // Navigation Properties
        public virtual ApplicationUser User { get; set; }
        public virtual Product Product { get; set; }
    }
}
