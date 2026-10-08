using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string dateFilter = "All")
    {
        var model = await _dashboardService.GetDashboardDataAsync(CurrentUserContext, dateFilter);
        return View(model);
    }
}
