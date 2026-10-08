using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class FollowUpsController : BaseController
{
    private readonly IFollowUpService _followUpService;
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IOpportunityService _opportunityService;
    private readonly IUserService _userService;

    public FollowUpsController(
        IFollowUpService followUpService,
        ICustomerService customerService,
        ILeadService leadService,
        IOpportunityService opportunityService,
        IUserService userService,
        ICurrentUserService currentUserService) : base(currentUserService)
    {
        _followUpService = followUpService;
        _customerService = customerService;
        _leadService = leadService;
        _opportunityService = opportunityService;
        _userService = userService;
    }

    private async Task PopulateDropdownsAsync(int? customerId = null, int? leadId = null, int? oppId = null, string? assignedTo = null)
    {
        var custs = await _customerService.GetCustomersAsync(new CustomerFilterDto { PageSize = 1000 }, CurrentUserContext);
        ViewBag.Customers = new SelectList(custs.Items, "CustomerId", "CustomerName", customerId);

        var leads = await _leadService.GetLeadsAsync(new LeadFilterDto { PageSize = 1000 }, CurrentUserContext);
        ViewBag.Leads = new SelectList(leads.Items, "LeadId", "LeadName", leadId);

        var opps = await _opportunityService.GetOpportunitiesAsync(new OpportunityFilterDto { PageSize = 1000 }, CurrentUserContext);
        ViewBag.Opportunities = new SelectList(opps.Items, "OpportunityId", "OpportunityName", oppId);

        if (CurrentUserContext.IsAdmin || CurrentUserContext.IsManager)
        {
            var reps = await _userService.GetSalesExecutivesAsync();
            ViewBag.AssignedUsers = new SelectList(reps, "Id", "FullName", assignedTo);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] FollowUpFilterDto filter)
    {
        var result = await _followUpService.GetFollowUpsAsync(filter, CurrentUserContext);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var model = new CreateFollowUpDto
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.UtcNow.Date.AddDays(1)
        };

        await PopulateDropdownsAsync(customerId, leadId, opportunityId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateFollowUpDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        var response = await _followUpService.CreateFollowUpAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to schedule follow-up.");
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Follow-up scheduled successfully.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var fu = await _followUpService.GetByIdAsync(id, CurrentUserContext);
        if (fu == null)
        {
            SetErrorNotification("Follow-up not found or access denied.");
            return RedirectToAction(nameof(Index));
        }

        var model = new UpdateFollowUpDto
        {
            FollowUpId = fu.FollowUpId,
            CustomerId = fu.CustomerId,
            LeadId = fu.LeadId,
            OpportunityId = fu.OpportunityId,
            FollowUpDate = fu.FollowUpDate,
            FollowUpType = fu.FollowUpType,
            Subject = fu.Subject,
            Remarks = fu.Remarks,
            Status = fu.Status,
            AssignedTo = fu.AssignedTo
        };

        await PopulateDropdownsAsync(fu.CustomerId, fu.LeadId, fu.OpportunityId, fu.AssignedTo);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateFollowUpDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        var response = await _followUpService.UpdateFollowUpAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to update follow-up.");
            await PopulateDropdownsAsync(model.CustomerId, model.LeadId, model.OpportunityId, model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Follow-up updated successfully.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteFollowUpDto model)
    {
        if (!ModelState.IsValid)
        {
            SetErrorNotification("Completion remarks are required.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _followUpService.CompleteFollowUpAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Failed to complete follow-up.");
        }
        else
        {
            SetSuccessNotification("Follow-up marked as Completed!");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(RescheduleFollowUpDto model)
    {
        if (!ModelState.IsValid)
        {
            SetErrorNotification("Valid date and reschedule reason are required.");
            return RedirectToAction(nameof(Index));
        }

        var response = await _followUpService.RescheduleFollowUpAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Failed to reschedule follow-up.");
        }
        else
        {
            SetSuccessNotification("Follow-up rescheduled successfully.");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var response = await _followUpService.CancelFollowUpAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Unable to cancel follow-up.");
        }
        else
        {
            SetSuccessNotification("Follow-up cancelled.");
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _followUpService.DeleteFollowUpAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Unable to delete follow-up.");
        }
        else
        {
            SetSuccessNotification("Follow-up deleted.");
        }
        return RedirectToAction(nameof(Index));
    }
}
