using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Auth;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("Invalid model state.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var user = await _userManager.FindByEmailAsync(model.Email.Trim()) ?? await _userManager.FindByNameAsync(model.Email.Trim());

        if (user == null)
        {
            await _auditService.LogAsync(null, model.Email, "Failed Login", "API Auth", null, null, "User not found", ip);
            return Unauthorized(ApiResponse<UserDto>.Fail("Invalid login credentials."));
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API Auth", user.Id, null, "Account deactivated", ip);
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<UserDto>.Fail("Account is deactivated."));
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            await _auditService.LogAsync(user.Id, user.Email, "Login", "API Auth", user.Id, null, "Successful API login", ip);

            var userDto = new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Department = user.Department,
                Role = roles.FirstOrDefault() ?? "SalesExecutive",
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            };

            return Ok(ApiResponse<UserDto>.Ok(userDto, "Authentication successful."));
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Lockout", "API Auth", user.Id, null, "Account locked out", ip);
            return StatusCode(StatusCodes.Status423Locked, ApiResponse<UserDto>.Fail("Account is locked due to too many failed attempts."));
        }

        await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API Auth", user.Id, null, "Invalid password", ip);
        return Unauthorized(ApiResponse<UserDto>.Fail("Invalid login credentials."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var context = _currentUserService.GetUserContext();
        await _signInManager.SignOutAsync();
        await _auditService.LogAsync(context.UserId, context.UserName, "Logout", "API Auth", context.UserId, null, "API user logged out", context.IpAddress);
        return Ok(ApiResponse<bool>.Ok(true, "Logged out successfully."));
    }
}
