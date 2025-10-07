using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Products
{
    public class UpdateProductRequest: CreateProductRequest
    {
        public int Id { get; set; }
    }
}
