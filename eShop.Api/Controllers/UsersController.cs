using eShop.Core.DTOs.Users;
using eShop.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var users = await _userManager.Users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserDto
                {
                    Id = user.Id,
                    FirstName = user.UserName,
                    Email = user.Email,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                });
            }

            return Ok(new
            {
                Users = userDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = await _userManager.Users.CountAsync()
            });
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = new UserDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList()
            };

            return Ok(userDto);
        }


        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return NoContent();
        }

        // POST: api/users/{id}/roles
        [HttpPost("{id}/roles")]
        public async Task<IActionResult> AssignRoles(string id, [FromBody] AssignRolesDto model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            foreach (var role in model.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    return BadRequest(new { Message = $"Role '{role}' does not exist" });

                if (!await _userManager.IsInRoleAsync(user, role))
                {
                    var result = await _userManager.AddToRoleAsync(user, role);
                    if (!result.Succeeded)
                        return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
                }
            }

            return Ok(new { Message = "Roles assigned successfully" });
        }

        // DELETE: api/users/{id}/roles/{roleName}
        [HttpDelete("{id}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRole(string id, string roleName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest(new { Message = $"Role '{roleName}' does not exist" });

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                    return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
            }

            return Ok(new { Message = "Role removed successfully" });
        }

        public class AssignRolesDto
        {
            public List<string> Roles { get; set; }
        }
    }
}