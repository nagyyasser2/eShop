using System.ComponentModel.DataAnnotations;

namespace eShop.Core.DTOs
{

    public class GoogleLoginRequest
    {
        [Required]
        public string AccessToken { get; set; }
    }

    public class GoogleCodeRequest
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string RedirectUri { get; set; }
    }

    public class GoogleAuthUrlRequest
    {
        [Required]
        public string RedirectUri { get; set; }

        public string? State { get; set; }
    }
}
