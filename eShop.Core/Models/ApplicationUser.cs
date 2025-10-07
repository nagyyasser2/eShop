using Microsoft.AspNetCore.Identity;

namespace eShop.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty.ToString();
        public string LastName { get; set; } = string.Empty.ToString();
        public string PhoneNumber {  get; set; } = string.Empty.ToString() ;
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
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
