using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Auth;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : BaseController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetAllUsersAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(UpdateUserRoleDto model)
    {
        if (!ModelState.IsValid)
        {
            SetErrorNotification("Invalid role update request.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _userService.UpdateUserRoleAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Failed to update user role.");
        }
        else
        {
            SetSuccessNotification("User role updated successfully.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var response = await _userService.ToggleUserActiveStatusAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Failed to modify user status.");
        }
        else
        {
            SetSuccessNotification(response.Message ?? "User status changed.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetUserPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            SetErrorNotification("Password must be at least 8 characters long.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _userService.ResetUserPasswordAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Password reset failed.");
        }
        else
        {
            SetSuccessNotification("User password reset successfully.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var response = await _userService.UnlockUserAccountAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Failed to unlock account.");
        }
        else
        {
            SetSuccessNotification("User account unlocked successfully.");
        }

        return RedirectToAction(nameof(Index));
    }
}
