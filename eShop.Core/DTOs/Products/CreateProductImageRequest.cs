using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs.Products
{
    public class CreateProductImageRequest
    {
        [Required]
        public IFormFile File { get; set; }

        public bool IsPrimary { get; set; }
    }
}
