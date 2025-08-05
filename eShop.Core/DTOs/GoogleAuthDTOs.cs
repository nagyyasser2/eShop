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
    public class GoogleTokenRequest
    {
        public string IdToken { get; set; }
    }

    public class GoogleUserInfo
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public bool VerifiedEmail { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Picture { get; set; }
    }
    public class GoogleJwtRequest
    {
        public string Credential { get; set; } = string.Empty;
    }
}
