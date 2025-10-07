using System.Text.Json.Serialization;

namespace eShop.Core.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool IsPrimary { get; set; } = false;
        public bool IsAttached { get; set; } = false;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public int ProductId { get; set; }
        [JsonIgnore]
        public virtual Product Product { get; set; } = null!;
    }
}
