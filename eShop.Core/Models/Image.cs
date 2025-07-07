using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Image
    {
        public int Id { get; set; }
        [Required]
        public string FileName { get; set; }
        [Required]
        public string FilePath { get; set; }
        public string? AltText { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
