using Microsoft.AspNetCore.Authorization;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using eShop.Core.Config;
using eShop.Core.Models;
using eShop.Core.DTOs;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IConfiguration _configuration;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IGoogleAuthService googleAuthService,
            IJwtTokenService jwtTokenService,
            IConfiguration configuration
            )
        {
            _googleAuthService = googleAuthService;
            _jwtTokenService = jwtTokenService;
            _signInManager = signInManager;
            _configuration = configuration;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);

            if (existingUser != null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User with this email already exists"
                });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DateOfBirth = request.DateOfBirth
            };

            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                var token = await _jwtTokenService.GenerateTokenAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "User registered successfully",
                    Data = new AuthResponse
                    {
                        Token = token,
                        User = new UserResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            DateOfBirth = user.DateOfBirth
                        }
                    }
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "User registration failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        [HttpGet("google-auth-url")]
        public IActionResult GetGoogleAuthUrl([FromQuery] GoogleAuthUrlRequest request)
        {
            var clientId = HttpContext.RequestServices.GetService<IConfiguration>()["Authentication:Google:ClientId"];
            var scopes = "openid profile email";
            var responseType = "code";
            var state = request.State ?? Guid.NewGuid().ToString();
            

            var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={Uri.EscapeDataString(clientId)}&" +
                         $"redirect_uri={Uri.EscapeDataString(request.RedirectUri)}&" +
                         $"response_type={responseType}&" +
                         $"scope={Uri.EscapeDataString(scopes)}&" +
                         $"state={Uri.EscapeDataString(state)}";

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Google auth URL generated successfully",
                Data = new { AuthUrl = authUrl, State = state }
            });
        }

        [HttpGet("callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Missing authorization code or redirect URI"
                    });
                }

                var googleSettings = _configuration.GetSection("Authentication:Google").Get<GoogleAuthSettings>();

                if (googleSettings == null || string.IsNullOrEmpty(googleSettings.RedirectUri))
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Google authentication settings are not configured properly."
                    });
                }

                var tokenResponse = await _googleAuthService.ExchangeCodeForTokenAsync(code, googleSettings.RedirectUri);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to exchange authorization code for access token"
                    });
                }

                var googleUser = await _googleAuthService.GetUserInfoAsync(tokenResponse.AccessToken);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to get user information from Google"
                    });
                }

                var user = await _userManager.FindByEmailAsync(googleUser.Email);

                if (user == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        UserName = googleUser.Email,
                        Email = googleUser.Email,
                        FirstName = googleUser.GivenName ?? "",
                        LastName = googleUser.FamilyName ?? "",
                        GoogleId = googleUser.Id,
                        IsGoogleUser = true,
                        ProfilePictureUrl = googleUser.Picture,
                        EmailConfirmed = googleUser.VerifiedEmail
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Failed to create user account",
                            Errors = result.Errors.Select(e => e.Description).ToList()
                        });
                    }

                    // Add user to default role
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    // Update existing user with Google info if not already a Google user
                    if (!user.IsGoogleUser)
                    {
                        user.GoogleId = googleUser.Id;
                        user.IsGoogleUser = true;
                        user.ProfilePictureUrl = googleUser.Picture;
                        await _userManager.UpdateAsync(user);
                    }
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Google authentication successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        User = new UserResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            DateOfBirth = user.DateOfBirth,
                            ProfilePictureUrl = user.ProfilePictureUrl,
                            Roles = roles.ToList()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Google authentication failed",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                var googleUser = await _googleAuthService.GetUserInfoAsync(request.AccessToken);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid Google access token"
                    });
                }

                var user = await _userManager.FindByEmailAsync(googleUser.Email);

                if (user == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        UserName = googleUser.Email,
                        Email = googleUser.Email,
                        FirstName = googleUser.GivenName ?? "",
                        LastName = googleUser.FamilyName ?? "",
                        GoogleId = googleUser.Id,
                        IsGoogleUser = true,
                        ProfilePictureUrl = googleUser.Picture,
                        EmailConfirmed = googleUser.VerifiedEmail
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Failed to create user account",
                            Errors = result.Errors.Select(e => e.Description).ToList()
                        });
                    }

                    // Add user to default role
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    // Update existing user with Google info if not already a Google user
                    if (!user.IsGoogleUser)
                    {
                        user.GoogleId = googleUser.Id;
                        user.IsGoogleUser = true;
                        user.ProfilePictureUrl = googleUser.Picture;
                        await _userManager.UpdateAsync(user);
                    }
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Google login successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        User = new UserResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            DateOfBirth = user.DateOfBirth,
                            ProfilePictureUrl = user.ProfilePictureUrl,
                            Roles = roles.ToList()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Google authentication failed",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("google-code")]
        public async Task<IActionResult> GoogleCodeLogin([FromBody] GoogleCodeRequest request)
        {
            try
            {
                Console.WriteLine(request.Code);
                Console.WriteLine(request.RedirectUri);

                var tokenResponse = await _googleAuthService.ExchangeCodeForTokenAsync(request.Code, request.RedirectUri);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to exchange authorization code for access token"
                    });
                }

                var googleUser = await _googleAuthService.GetUserInfoAsync(tokenResponse.AccessToken);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Failed to get user information from Google"
                    });
                }

                var user = await _userManager.FindByEmailAsync(googleUser.Email);

                if (user == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        UserName = googleUser.Email,
                        Email = googleUser.Email,
                        FirstName = googleUser.GivenName ?? "",
                        LastName = googleUser.FamilyName ?? "",
                        GoogleId = googleUser.Id,
                        IsGoogleUser = true,
                        ProfilePictureUrl = googleUser.Picture,
                        EmailConfirmed = googleUser.VerifiedEmail
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Failed to create user account",
                            Errors = result.Errors.Select(e => e.Description).ToList()
                        });
                    }

                    // Add user to default role
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    // Update existing user with Google info if not already a Google user
                    if (!user.IsGoogleUser)
                    {
                        user.GoogleId = googleUser.Id;
                        user.IsGoogleUser = true;
                        user.ProfilePictureUrl = googleUser.Picture;
                        await _userManager.UpdateAsync(user);
                    }
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Google login successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        User = new UserResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            DateOfBirth = user.DateOfBirth,
                            ProfilePictureUrl = user.ProfilePictureUrl,
                            Roles = roles.ToList()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Google authentication failed",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("google-jwt")]
        public async Task<IActionResult> GoogleJwtLogin([FromBody] GoogleJwtRequest request)
        {
            try
            {
                // Validate the Google JWT token
                var googleUser = await _googleAuthService.ValidateGoogleJwtAsync(request.Credential);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid Google JWT token"
                    });
                }

                var user = await _userManager.FindByEmailAsync(googleUser.Email);

                if (user == null)
                {
                    // Create new user
                    user = new ApplicationUser
                    {
                        UserName = googleUser.Email,
                        Email = googleUser.Email,
                        FirstName = googleUser.GivenName ?? "",
                        LastName = googleUser.FamilyName ?? "",
                        GoogleId = googleUser.Id,
                        IsGoogleUser = true,
                        ProfilePictureUrl = googleUser.Picture,
                        EmailConfirmed = googleUser.VerifiedEmail
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new ApiResponse<object>
                        {
                            Success = false,
                            Message = "Failed to create user account",
                            Errors = result.Errors.Select(e => e.Description).ToList()
                        });
                    }

                    // Add user to default role
                    await _userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    // Update existing user with Google info if not already a Google user
                    if (!user.IsGoogleUser)
                    {
                        user.GoogleId = googleUser.Id;
                        user.IsGoogleUser = true;
                        user.ProfilePictureUrl = googleUser.Picture;
                        await _userManager.UpdateAsync(user);
                    }
                }

                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Google authentication successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        User = new UserResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            DateOfBirth = user.DateOfBirth,
                            ProfilePictureUrl = user.ProfilePictureUrl,
                            Roles = roles.ToList()
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Google authentication failed",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (result.Succeeded)
            {
                var token = await _jwtTokenService.GenerateTokenAsync(user);
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = new AuthResponse
                    {
                        Token = token,
                        User = new UserResponse
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            DateOfBirth = user.DateOfBirth,
                            Roles = roles.ToList()
                        }
                    }
                });
            }

            if (result.IsLockedOut)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Account is locked out"
                });
            }

            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid email or password"
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Logout successful"
            });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password changed successfully"
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Password change failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                Message = "Profile retrieved successfully",
                Data = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    DateOfBirth = user.DateOfBirth,
                    Roles = roles.ToList()
                }
            });
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.DateOfBirth = request.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new ApiResponse<UserResponse>
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Data = new UserResponse
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        DateOfBirth = user.DateOfBirth,
                        Roles = roles.ToList()
                    }
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Profile update failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (!await _roleManager.RoleExistsAsync(request.Role))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Role does not exist"
                });
            }

            var result = await _userManager.AddToRoleAsync(user, request.Role);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = $"Role '{request.Role}' assigned to user successfully"
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Role assignment failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }
    }
}
