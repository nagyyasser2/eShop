using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Variants
{
    public class UpdateVariantRequest
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
}
