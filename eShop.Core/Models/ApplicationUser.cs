using Microsoft.AspNetCore.Identity;

namespace eShop.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public bool IsGoogleUser { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? GoogleId { get; set; }

        // Navigation Properties
        public virtual Cart? Cart { get; set; }
        public virtual WishList? WishList { get; set; }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
