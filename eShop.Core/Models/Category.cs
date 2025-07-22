using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Self-referencing for nesting
        public int? ParentCategoryId { get; set; }

        public virtual Category? ParentCategory { get; set; }

        public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();

        // Navigation to products
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
