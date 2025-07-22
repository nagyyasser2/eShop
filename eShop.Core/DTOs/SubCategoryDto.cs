using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class SubCategoryDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; }
        public IList<IFormFile>? ImageFiles { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
        [Required]
        public int CategoryId { get; set; }
    }
}
