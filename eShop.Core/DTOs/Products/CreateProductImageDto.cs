using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs.Products
{
    public class CreateProductImageDto
    {
        public string Path { get; set; } = string.Empty;
        public bool? IsPrimary { get; set; } 
        public bool? IsAttached { get; set; } 
        public int ProductId { get; set; }
    }
}
