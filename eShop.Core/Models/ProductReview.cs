namespace eShop.Core.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        // Foreign Keys
        public int ProductId { get; set; }
        public int ReviewId { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; }
        public virtual Review Review { get; set; }
    }
}
