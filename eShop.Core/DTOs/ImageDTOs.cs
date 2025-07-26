using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{
    public class CreateImageDTO
    {
        [Required]
        public string Url { get; set; }
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        [Required]
        public int ProductId { get; set; }
    }

    public class UpdateImageDTO
    {
        [Required]
        public int Id { get; set; }
        public string? Url { get; set; }
        public string? AltText { get; set; }
        public bool? IsPrimary { get; set; }
        public int? SortOrder { get; set; }
    }

    public class ImageDTO
    {
        public int Id { get; set; }
        public string Url { get; set; }
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ProductId { get; set; }
    }
}
