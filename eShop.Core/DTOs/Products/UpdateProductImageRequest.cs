using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs.Products
{
    public class UpdateProductImageRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public bool IsPrimary { get; set; }
        [Required]
        public bool IsDeletable { get; set; }
        public string? Path { get; set; }
        public IFormFile? File { get; set; }
    }
}
