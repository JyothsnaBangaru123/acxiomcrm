using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs.Activities;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ActivitiesController : BaseController
{
    private readonly IActivityService _activityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public ActivitiesController(
        IActivityService activityService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService,
        ICurrentUserService currentUserService) : base(currentUserService)
    {
        _activityService = activityService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, string? assignedTo = null)
    {
        var custs = await _customerService.GetCustomersAsync(new CustomerFilterDto { PageSize = 1000 }, CurrentUserContext);
        ViewBag.Customers = new SelectList(custs.Items, "CustomerId", "CustomerName", customerId);

        var leads = await _leadService.GetLeadsAsync(new LeadFilterDto { PageSize = 1000 }, CurrentUserContext);
        ViewBag.Leads = new SelectList(leads.Items, "LeadId", "LeadName", leadId);

        if (CurrentUserContext.IsAdmin || CurrentUserContext.IsManager)
        {
            var reps = await _userService.GetSalesExecutivesAsync();
            ViewBag.AssignedUsers = new SelectList(reps, "Id", "FullName", assignedTo);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ActivityFilterDto filter)
    {
        var result = await _activityService.GetActivitiesAsync(filter, CurrentUserContext);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var model = new CreateActivityDto
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow
        };

        await PopulateDropdownsAsync(customerId, leadId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateActivityDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        var response = await _activityService.CreateActivityAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to log activity.");
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Activity logged successfully.");
        return RedirectToAction(nameof(Index));
    }
}
