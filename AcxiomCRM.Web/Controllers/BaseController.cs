using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public abstract class BaseController : Controller
{
    private readonly ICurrentUserService _currentUserService;

    protected BaseController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    protected UserContext CurrentUserContext => _currentUserService.GetUserContext();

    protected void SetSuccessNotification(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    protected void SetErrorNotification(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    protected void SetWarningNotification(string message)
    {
        TempData["WarningMessage"] = message;
    }
}
