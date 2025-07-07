using System.ComponentModel.DataAnnotations.Schema;

namespace eShop.Core.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }

        // Navigation Properties
        public virtual Cart Cart { get; set; }
        public virtual Product Product { get; set; }
        public virtual ProductVariant? ProductVariant { get; set; }
    }
}
