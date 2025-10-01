using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
    }
}
