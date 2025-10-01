using System.ComponentModel.DataAnnotations.Schema;


namespace eShop.Core.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        public string ProductName { get; set; } 
        public string? ProductSKU { get; set; }

        // Foreign Keys
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
        public virtual Variant? ProductVariant { get; set; }
    }
}
