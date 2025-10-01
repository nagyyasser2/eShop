using System.ComponentModel.DataAnnotations;

namespace eShop.Core.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime? RevokedAt { get; set; }

        public string? RevokedByIp { get; set; }

        public string? CreatedByIp { get; set; }

        // Navigation property
        public ApplicationUser User { get; set; } = null!;

        // Computed properties
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
