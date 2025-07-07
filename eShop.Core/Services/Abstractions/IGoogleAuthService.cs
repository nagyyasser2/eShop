
using eShop.Core.Models;

namespace eShop.Core.Services.Abstractions
{
    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> GetUserInfoAsync(string accessToken);
        Task<GoogleTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri);
        Task<GoogleTokenResponse> RefreshTokenAsync(string refreshToken);
    }
}
