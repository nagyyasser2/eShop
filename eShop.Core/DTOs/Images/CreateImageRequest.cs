using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Images
{
    public class CreateImageRequest
    {
        [Required]
        public string Path { get; set; } = string.Empty;
        public string? AltText { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public bool IsAttached { get; set; } = false;
    }
}
