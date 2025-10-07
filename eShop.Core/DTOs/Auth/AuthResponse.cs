using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Auth
{
    public class AuthResponse
    {
        public string Token { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public UserResponse User { get; set; } = new UserResponse();
    }
     
}
