using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Auth;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Enums;

namespace AcxiomCRM.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IAuditService _auditService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.ToListAsync(cancellationToken);
        var list = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            list.Add(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Department = user.Department,
                Role = roles.FirstOrDefault() ?? "None",
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                CreatedDate = user.CreatedDate,
                LastLoginDate = user.LastLoginDate
            });
        }

        return list.OrderBy(u => u.FullName).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var isLocked = await _userManager.IsLockedOutAsync(user);

        return new UserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Department = user.Department,
            Role = roles.FirstOrDefault() ?? "None",
            IsActive = user.IsActive,
            IsLockedOut = isLocked,
            CreatedDate = user.CreatedDate,
            LastLoginDate = user.LastLoginDate
        };
    }

    public async Task<ApiResponse<bool>> UpdateUserRoleAsync(UpdateUserRoleDto dto, UserContext currentUserContext, CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAdmin)
        {
            return ApiResponse<bool>.Fail("Only administrators can update user roles.");
        }

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("User not found.");
        }

        if (!AppRoles.AllRoles.Contains(dto.Role))
        {
            return ApiResponse<bool>.Fail($"Invalid role '{dto.Role}'.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var oldRole = currentRoles.FirstOrDefault() ?? "None";

        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, dto.Role);

        await _auditService.LogAsync(
            currentUserContext.UserId,
            currentUserContext.UserName,
            "RoleChange",
            "User",
            user.Id,
            $"Role: {oldRole}",
            $"Role changed to: {dto.Role}",
            currentUserContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, $"Role updated to {dto.Role} successfully.");
    }

    public async Task<ApiResponse<bool>> ToggleUserActiveStatusAsync(string userId, UserContext currentUserContext, CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAdmin)
        {
            return ApiResponse<bool>.Fail("Only administrators can modify user activation status.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("User not found.");
        }

        if (user.Id == currentUserContext.UserId)
        {
            return ApiResponse<bool>.Fail("You cannot deactivate your own administrative account.");
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(
            currentUserContext.UserId,
            currentUserContext.UserName,
            "Security",
            "User",
            user.Id,
            $"Active: {!user.IsActive}",
            $"Active status changed to: {user.IsActive}",
            currentUserContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, $"User is now {(user.IsActive ? "active" : "inactive")}.");
    }

    public async Task<ApiResponse<bool>> ResetUserPasswordAsync(ResetUserPasswordDto dto, UserContext currentUserContext, CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAdmin)
        {
            return ApiResponse<bool>.Fail("Only administrators can reset user passwords.");
        }

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("User not found.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            return ApiResponse<bool>.Fail("Password reset failed.", result.Errors.Select(e => e.Description).ToList());
        }

        await _auditService.LogAsync(
            currentUserContext.UserId,
            currentUserContext.UserName,
            "Security",
            "User",
            user.Id,
            null,
            "Administrative password reset performed (no secret stored).",
            currentUserContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "Password has been reset successfully.");
    }

    public async Task<ApiResponse<bool>> UnlockUserAccountAsync(string userId, UserContext currentUserContext, CancellationToken cancellationToken = default)
    {
        if (!currentUserContext.IsAdmin)
        {
            return ApiResponse<bool>.Fail("Only administrators can unlock user accounts.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<bool>.Fail("User not found.");
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            currentUserContext.UserId,
            currentUserContext.UserName,
            "Security",
            "User",
            user.Id,
            "Status: Locked",
            "Account unlocked by Administrator",
            currentUserContext.IpAddress,
            cancellationToken);

        return ApiResponse<bool>.Ok(true, "User account unlocked successfully.");
    }

    public async Task<IReadOnlyList<UserDto>> GetSalesExecutivesAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).ToListAsync(cancellationToken);
        var list = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains(AppRoles.SalesExecutive) || roles.Contains(AppRoles.Manager) || roles.Contains(AppRoles.Admin))
            {
                list.Add(new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "None"
                });
            }
        }

        return list.OrderBy(u => u.FullName).ToList();
    }
}
