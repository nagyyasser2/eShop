using eShop.Core.Models;
using System.Security.Claims;

namespace eShop.Core.Services.Abstractions
{
    public interface IJwtTokenService
    {
        Task<string> GenerateTokenAsync(ApplicationUser user);
        Task<string> GenerateRefreshTokenAsync();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
