
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class CreateVariantDTO
    {
        [Required, MaxLength(100)]
        public string SKU { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }
        public int StockQuantity { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string Color { get; set; }
        public string Size { get; set; }
        [Required]
        public int ProductId { get; set; } 
    }

    public class UpdateVariantDTO
    {
        [Required]
        public int Id { get; set; }
        [MaxLength(100)]
        public string? SKU { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsActive { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
    }

    public class VariantDTO
    {
        public int Id { get; set; }
        public string SKU { get; set; }
        public decimal? Price { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductId { get; set; }
    }
}
