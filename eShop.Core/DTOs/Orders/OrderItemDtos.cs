using eShop.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs.Orders
{
    public class CreateOrderItemDto
    {
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; } 
        public int? ProductVariantId { get; set; }
    }

    public class UpdateOrderItemDto
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string ProductName { get; set; }
        public string? ProductSku { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public Order Order { get; set; }
        public Product Product { get; set; }
        public Variant ProductVariant { get; set; }
    }
}
