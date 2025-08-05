using eShop.Core.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using eShop.Core.Models;


namespace eShop.Core.Services.Implementations
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GoogleAuthService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<GoogleUserInfo>(json, options);
        }

        public async Task<GoogleTokenResponse> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];

            var parameters = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(parameters)
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<GoogleTokenResponse>(json, options);
        }

        public async Task<GoogleTokenResponse> RefreshTokenAsync(string refreshToken)
        {
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];

            var parameters = new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "refresh_token"
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(parameters)
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            return JsonSerializer.Deserialize<GoogleTokenResponse>(json, options);
        }

        public async Task<GoogleUserInfo?> ValidateGoogleJwtAsync(string jwtToken)
        {
            try
            {
                using var httpClient = new HttpClient();

                // Verify the token with Google
                var response = await httpClient.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={jwtToken}");

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var tokenInfo = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent);

                if (tokenInfo == null)
                {
                    return null;
                }

                var clientId = _configuration["Authentication:Google:ClientId"];

                // Verify the audience (your client ID)
                if (!tokenInfo.ContainsKey("aud") ||
                    tokenInfo["aud"].ToString() != clientId)
                {
                    return null;
                }

                // Extract user information
                return new GoogleUserInfo
                {
                    Id = tokenInfo.GetValueOrDefault("sub")?.ToString() ?? "",
                    Email = tokenInfo.GetValueOrDefault("email")?.ToString() ?? "",
                    VerifiedEmail = bool.Parse(tokenInfo.GetValueOrDefault("email_verified")?.ToString() ?? "false"),
                    GivenName = tokenInfo.GetValueOrDefault("given_name")?.ToString() ?? "",
                    FamilyName = tokenInfo.GetValueOrDefault("family_name")?.ToString() ?? "",
                    Picture = tokenInfo.GetValueOrDefault("picture")?.ToString() ?? ""
                };
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error validating Google JWT: {ex.Message}");
                return null;
            }
        }
    }

}
