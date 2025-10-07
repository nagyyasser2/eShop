using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Emails
{
    public class ResendEmailConfirmationRequestDetailed
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Client-side generated request ID for tracking/debugging
        /// </summary>
        [StringLength(50, ErrorMessage = "Request ID cannot exceed 50 characters")]
        public string? RequestId { get; set; }

        /// <summary>
        /// Optional: Source of the request (web, mobile app, etc.)
        /// </summary>
        [StringLength(50, ErrorMessage = "Source cannot exceed 50 characters")]
        public string? Source { get; set; }
    }
}
