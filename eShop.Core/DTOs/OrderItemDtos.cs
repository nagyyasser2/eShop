
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class OrderItemDto
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string ProductName { get; set; }
        public string? ProductSKU { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
    }
    public class CreateOrderItemDto
    {
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string ProductName { get; set; }
        public string? ProductSKU { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "The ProductId filed is required.")]
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
    }


}
