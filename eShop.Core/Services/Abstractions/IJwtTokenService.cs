using eShop.Core.Models;
using eShop.Core.DTOs.Auth;
using System.Security.Claims;

namespace eShop.Core.Services.Abstractions
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
        Task<AuthResponse> GenerateTokensAsync(ApplicationUser user);
        Task<AuthResponse> RefreshTokenAsync(string accessToken, string refreshToken);
    }
}
