using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Users
{
    public class UserDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; } 
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public List<string>? Roles { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsGoogleUser { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
