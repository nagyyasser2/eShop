using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Image
    {
        public int Id { get; set; }

        [Required]
        public string Path { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;

        [Required]
        public bool IsAttached { get; set; } = false;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    }
}
