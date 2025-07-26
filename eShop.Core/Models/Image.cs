using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Image
    {
        public int Id { get; set; }
        [Required]
        public string Url { get; set; }
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public int ProductId { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; }
       
    }

}
