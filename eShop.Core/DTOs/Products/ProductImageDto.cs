using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Products
{
    public class ProductImageDto
    {
        public int Id { get; set; }
        public string Path { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsDeletable { get; set; }
        public bool IsAttached { get; set; }
        public int ProductId { get; set; }
    }
}
