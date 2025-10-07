using Microsoft.AspNetCore.Authorization;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using eShop.Core.Configurations;
using Microsoft.AspNetCore.Mvc;
using eShop.Core.DTOs.Emails;
using eShop.Core.DTOs.Google;
using eShop.Core.Templates;
using eShop.Core.DTOs.Auth;
using eShop.Core.DTOs.Api;
using eShop.Core.Models;


namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IGoogleAuthService googleAuthService,
        IJwtTokenService jwtTokenService,
        IEmailSender emailSender,
        IOptions<FrontendConfiguration> frontendConfiguration
            ) : ControllerBase
    {
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

            var existingUser = await userManager.FindByEmailAsync(request.Email);

            if (existingUser != null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email already exists."
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

            var result = await userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "User");

                // Generate email confirmation token
                var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
                var frontendConfigurationValue = frontendConfiguration.Value;
                var confirmationLink = $"{frontendConfigurationValue.Url}/confirmEmail?userId={user.Id}&token={Uri.EscapeDataString(emailToken)}";

                // Send confirmation email
                var emailContent = EmailTemplate.GetEmailConfirmationTemplate(
                    user.FirstName,
                    confirmationLink);

                await emailSender.SendEmailAsync(user.Email, "Confirm Your Email", emailContent);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Successfully registered. Please check your email to confirm your account."
                });
            }


            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "User registration failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
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

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid email or password"
                });
            }

            // Check if email is confirmed (only for non-Google users)
            if (!user.IsGoogleUser && !user.EmailConfirmed)
            {
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email not confirmed. Please check your email and confirm your account."
                });
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);

            if (result.Succeeded)
            {
                var authResponse = await jwtTokenService.GenerateTokensAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = authResponse
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

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid email confirmation request"
                });
            }

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Email is already confirmed"
                });
            }

            var result = await userManager.ConfirmEmailAsync(user, request.Token);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Email confirmed successfully"
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Email confirmation failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }
        
        [HttpPost("resend-email-confirmation")]
        public async Task<IActionResult> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequestDetailed request)
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

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If the email exists in our system, a confirmation email has been sent."
                });
            }

            if (user.EmailConfirmed)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Email is already confirmed"
                });
            }

            // Generate new email confirmation token
            var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                new { userId = user.Id, token = emailToken },
                Request.Scheme);

            // Send confirmation email
            var emailContent = EmailTemplate.GetEmailConfirmationTemplate(
                user.FirstName,
                confirmationLink);

            await emailSender.SendEmailAsync(user.Email, "Confirm Your Email", emailContent);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Confirmation email sent successfully"
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await userManager.GetUserAsync(User);
            if (user != null)
            {
                // Clear refresh token
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow;
                await userManager.UpdateAsync(user);
            }

            await signInManager.SignOutAsync();
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

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

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
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            var roles = await userManager.GetRolesAsync(user);

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
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Address = user.Address,
                    City = user.City,
                    State = user.State,
                    ZipCode = user.ZipCode,
                    Country = user.Country,
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

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // Check if at least one field is provided
            if (request.FirstName == null && request.LastName == null && request.DateOfBirth == null &&
                request.Address == null && request.City == null && request.State == null &&
                request.ZipCode == null && request.Country == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "At least one field is required"
                });
            }

            // Update basic profile fields
            if (request.FirstName != null)
            {
                user.FirstName = request.FirstName;
            }
            if (request.LastName != null)
            {
                user.LastName = request.LastName;
            }
            if (request.DateOfBirth != null)
            {
                user.DateOfBirth = request.DateOfBirth.Value;
            }

            // Update address fields
            if (request.Address != null)
            {
                user.Address = request.Address;
            }
            if (request.City != null)
            {
                user.City = request.City;
            }
            if (request.State != null)
            {
                user.State = request.State;
            }
            if (request.ZipCode != null)
            {
                user.ZipCode = request.ZipCode;
            }
            if (request.Country != null)
            {
                user.Country = request.Country;
            }

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var roles = await userManager.GetRolesAsync(user);
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
                        ProfilePictureUrl = user.ProfilePictureUrl,
                        Address = user.Address,
                        City = user.City,
                        State = user.State,
                        ZipCode = user.ZipCode,
                        Country = user.Country,    
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
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            if (!await roleManager.RoleExistsAsync(request.Role))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Role does not exist"
                });
            }

            var result = await userManager.AddToRoleAsync(user, request.Role);

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

        [HttpPost("google-jwt")]
        public async Task<IActionResult> GoogleJwtLogin([FromBody] GoogleJwtRequest request)
        {
            try
            {
                // Validate the Google JWT token
                var googleUser = await googleAuthService.ValidateGoogleJwtAsync(request.Credential);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invalid Google JWT token"
                    });
                }

                var user = await userManager.FindByEmailAsync(googleUser.Email);

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

                    var result = await userManager.CreateAsync(user);
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
                    await userManager.AddToRoleAsync(user, "User");
                }
                else
                {
                    // Update existing user with Google info if not already a Google user
                    if (!user.IsGoogleUser)
                    {
                        user.GoogleId = googleUser.Id;
                        user.IsGoogleUser = true;
                        user.ProfilePictureUrl = googleUser.Picture;
                        await userManager.UpdateAsync(user);
                    }
                }

                var authResponse = await jwtTokenService.GenerateTokensAsync(user);

                return Ok(new ApiResponse<AuthResponse>
                {
                    Success = true,
                    Message = "Google authentication successful",
                    Data = authResponse
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

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.AccessToken) || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Access token and refresh token are required"
                });
            }

            var authResponse = await jwtTokenService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);

            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = authResponse
            });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
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

            var user = await userManager.FindByEmailAsync(request.Email);

            // Don't reveal whether the user exists or not for security reasons
            if (user == null)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "If the email exists in our system, a password reset link has been sent."
                });
            }

            // Don't allow password reset for Google users
            if (user.IsGoogleUser)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Google users cannot reset their password. Please sign in with Google."
                });
            }

            // Generate password reset token
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var frontendConfigurationValue = frontendConfiguration.Value;
            var resetLink = $"{frontendConfigurationValue.Url}/reset-password?userId={user.Id}&token={Uri.EscapeDataString(resetToken)}";

            // Send password reset email
            var emailContent = EmailTemplate.GetPasswordResetTemplate(
                user.FirstName,
                resetLink);

            await emailSender.SendEmailAsync(user.Email, "Reset Your Password", emailContent);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "If the email exists in our system, a password reset link has been sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
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

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid password reset request"
                });
            }

            // Don't allow password reset for Google users
            if (user.IsGoogleUser)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Google users cannot reset their password."
                });
            }

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Password has been reset successfully. You can now login with your new password."
                });
            }

            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Password reset failed",
                Errors = result.Errors.Select(e => e.Description).ToList()
            });
        }

    }
}