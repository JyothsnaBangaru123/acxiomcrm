using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class LeadsController : BaseController
{
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;

    public LeadsController(
        ILeadService leadService,
        IUserService userService,
        ICurrentUserService currentUserService) : base(currentUserService)
    {
        _leadService = leadService;
        _userService = userService;
    }

    private async Task PopulateAssignedUsersDropdownAsync(string? selectedUserId = null)
    {
        if (CurrentUserContext.IsAdmin || CurrentUserContext.IsManager)
        {
            var reps = await _userService.GetSalesExecutivesAsync();
            ViewBag.AssignedUsers = new SelectList(reps, "Id", "FullName", selectedUserId);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] LeadFilterDto filter)
    {
        var result = await _leadService.GetLeadsAsync(filter, CurrentUserContext);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetLeadDetailsAsync(id, CurrentUserContext);
        if (lead == null)
        {
            SetErrorNotification("Lead not found or access denied.");
            return RedirectToAction(nameof(Index));
        }
        return View(lead);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateAssignedUsersDropdownAsync();
        return View(new CreateLeadDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLeadDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAssignedUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        var response = await _leadService.CreateLeadAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to create lead.");
            await PopulateAssignedUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Lead created successfully.");
        return RedirectToAction(nameof(Details), new { id = response.Data!.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, CurrentUserContext);
        if (lead == null)
        {
            SetErrorNotification("Lead not found or access denied.");
            return RedirectToAction(nameof(Index));
        }

        if (lead.Status == LeadStatus.Converted)
        {
            SetWarningNotification("Converted leads cannot be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new UpdateLeadDto
        {
            LeadId = lead.LeadId,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes
        };

        await PopulateAssignedUsersDropdownAsync(model.AssignedTo);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateLeadDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAssignedUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        var response = await _leadService.UpdateLeadAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to update lead.");
            await PopulateAssignedUsersDropdownAsync(model.AssignedTo);
            return View(model);
        }

        SetSuccessNotification("Lead updated successfully.");
        return RedirectToAction(nameof(Details), new { id = model.LeadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, LeadStatus newStatus)
    {
        var response = await _leadService.UpdateStatusAsync(id, newStatus, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Failed to update lead status.");
        }
        else
        {
            SetSuccessNotification($"Lead status updated to {newStatus}.");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetByIdAsync(id, CurrentUserContext);
        if (lead == null)
        {
            SetErrorNotification("Lead not found or access denied.");
            return RedirectToAction(nameof(Index));
        }

        if (lead.Status == LeadStatus.Converted)
        {
            SetWarningNotification("Lead is already converted.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new ConvertLeadDto
        {
            LeadId = lead.LeadId,
            OpportunityName = $"{lead.LeadName} - Contract",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 10000m,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30),
            OpportunityStage = OpportunityStage.Qualification,
            Probability = 20,
            CreateOpportunity = true
        };

        ViewBag.Lead = lead;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertLeadDto model)
    {
        if (!ModelState.IsValid)
        {
            var lead = await _leadService.GetByIdAsync(model.LeadId, CurrentUserContext);
            ViewBag.Lead = lead;
            return View(model);
        }

        var response = await _leadService.ConvertLeadAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Lead conversion failed.");
            var lead = await _leadService.GetByIdAsync(model.LeadId, CurrentUserContext);
            ViewBag.Lead = lead;
            return View(model);
        }

        SetSuccessNotification("Lead converted to Customer successfully!");
        return RedirectToAction("Details", "Customers", new { id = response.Data });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _leadService.DeleteLeadAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Unable to mark lead as lost.");
        }
        else
        {
            SetSuccessNotification("Lead marked as Lost.");
        }
        return RedirectToAction(nameof(Index));
    }
}
