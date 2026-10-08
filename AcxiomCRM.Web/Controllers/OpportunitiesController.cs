using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class OpportunitiesController : BaseController
{
    private readonly IOpportunityService _opportunityService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        ICustomerService customerService,
        ILeadService leadService,
        IUserService userService,
        ICurrentUserService currentUserService) : base(currentUserService)
    {
        _opportunityService = opportunityService;
        _customerService = customerService;
        _leadService = leadService;
        _userService = userService;
    }

    private async Task PopulateDropdownsAsync(int? selectedCustomerId = null, string? selectedUserId = null)
    {
        var custResult = await _customerService.GetCustomersAsync(new CustomerFilterDto { PageSize = 1000 }, CurrentUserContext);
        ViewBag.Customers = new SelectList(custResult.Items, "CustomerId", "CustomerName", selectedCustomerId);

        if (CurrentUserContext.IsAdmin || CurrentUserContext.IsManager)
        {
            var reps = await _userService.GetSalesExecutivesAsync();
            ViewBag.AssignedUsers = new SelectList(reps, "Id", "FullName", selectedUserId);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] OpportunityFilterDto filter)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(filter, CurrentUserContext);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opp = await _opportunityService.GetOpportunityDetailsAsync(id, CurrentUserContext);
        if (opp == null)
        {
            SetErrorNotification("Opportunity not found or access denied.");
            return RedirectToAction(nameof(Index));
        }
        return View(opp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var model = new CreateOpportunityDto
        {
            CustomerId = customerId ?? 0,
            LeadId = leadId,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        await PopulateDropdownsAsync(customerId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOpportunityDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
            return View(model);
        }

        var response = await _opportunityService.CreateOpportunityAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to create opportunity.");
            await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Opportunity created successfully.");
        return RedirectToAction(nameof(Details), new { id = response.Data!.OpportunityId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opp = await _opportunityService.GetByIdAsync(id, CurrentUserContext);
        if (opp == null)
        {
            SetErrorNotification("Opportunity not found or access denied.");
            return RedirectToAction(nameof(Index));
        }

        var model = new UpdateOpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            Notes = opp.Notes
        };

        await PopulateDropdownsAsync(opp.CustomerId, opp.AssignedTo);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateOpportunityDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
            return View(model);
        }

        var response = await _opportunityService.UpdateOpportunityAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to update opportunity.");
            await PopulateDropdownsAsync(model.CustomerId, model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Opportunity updated successfully.");
        return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _opportunityService.DeleteOpportunityAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Unable to mark opportunity as lost.");
        }
        else
        {
            SetSuccessNotification("Opportunity marked as Lost.");
        }
        return RedirectToAction(nameof(Index));
    }
}
