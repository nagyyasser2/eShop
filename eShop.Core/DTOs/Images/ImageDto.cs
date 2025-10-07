using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Images
{
    public class ImageDto
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool IsAttached { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
