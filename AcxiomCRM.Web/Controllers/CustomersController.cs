using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class CustomersController : BaseController
{
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;

    public CustomersController(
        ICustomerService customerService,
        IUserService userService,
        ICurrentUserService currentUserService) : base(currentUserService)
    {
        _customerService = customerService;
        _userService = userService;
    }

    private async Task PopulateOwnersDropdownAsync(string? selectedOwnerId = null)
    {
        if (CurrentUserContext.IsAdmin || CurrentUserContext.IsManager)
        {
            var reps = await _userService.GetSalesExecutivesAsync();
            ViewBag.Owners = new SelectList(reps, "Id", "FullName", selectedOwnerId);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] CustomerFilterDto filter)
    {
        var result = await _customerService.GetCustomersAsync(filter, CurrentUserContext);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetCustomerDetailsAsync(id, CurrentUserContext);
        if (customer == null)
        {
            SetErrorNotification("Customer not found or access denied.");
            return RedirectToAction(nameof(Index));
        }
        return View(customer);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateOwnersDropdownAsync();
        return View(new CreateCustomerDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCustomerDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOwnersDropdownAsync(model.OwnerId);
            return View(model);
        }

        var response = await _customerService.CreateCustomerAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to create customer.");
            await PopulateOwnersDropdownAsync(model.OwnerId);
            return View(model);
        }

        SetSuccessNotification("Customer created successfully.");
        return RedirectToAction(nameof(Details), new { id = response.Data!.CustomerId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetByIdAsync(id, CurrentUserContext);
        if (customer == null)
        {
            SetErrorNotification("Customer not found or access denied.");
            return RedirectToAction(nameof(Index));
        }

        var model = new UpdateCustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            OwnerId = customer.OwnerId
        };

        await PopulateOwnersDropdownAsync(model.OwnerId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCustomerDto model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOwnersDropdownAsync(model.OwnerId);
            return View(model);
        }

        var response = await _customerService.UpdateCustomerAsync(model, CurrentUserContext);
        if (!response.Success)
        {
            ModelState.AddModelError(string.Empty, response.Message ?? "Failed to update customer.");
            await PopulateOwnersDropdownAsync(model.OwnerId);
            return View(model);
        }

        SetSuccessNotification("Customer updated successfully.");
        return RedirectToAction(nameof(Details), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _customerService.DeleteCustomerAsync(id, CurrentUserContext);
        if (!response.Success)
        {
            SetErrorNotification(response.Message ?? "Unable to deactivate customer.");
        }
        else
        {
            SetSuccessNotification("Customer deactivated successfully.");
        }
        return RedirectToAction(nameof(Index));
    }
}
