using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Variants
{
    public class VariantDto
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
