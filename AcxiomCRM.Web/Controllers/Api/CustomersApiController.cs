using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Customers;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ICurrentUserService _currentUserService;

    public CustomersController(ICustomerService customerService, ICurrentUserService currentUserService)
    {
        _customerService = customerService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerDto>>> GetCustomers([FromQuery] CustomerFilterDto filter)
    {
        var result = await _customerService.GetCustomersAsync(filter, _currentUserService.GetUserContext());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerById(int id)
    {
        var result = await _customerService.GetCustomerDetailsAsync(id, _currentUserService.GetUserContext());
        if (result == null)
        {
            return NotFound(ApiResponse<CustomerDetailDto>.Fail("Customer not found or unauthorized."));
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> CreateCustomer([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _customerService.CreateCustomerAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetCustomerById), new { id = response.Data!.CustomerId }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (id != dto.CustomerId)
        {
            return BadRequest(ApiResponse<CustomerDto>.Fail("Route ID and payload ID do not match."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _customerService.UpdateCustomerAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            if (response.Message?.Contains("Unauthorized") == true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCustomer(int id)
    {
        var response = await _customerService.DeleteCustomerAsync(id, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            if (response.Message?.Contains("Unauthorized") == true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, response);
            }
            return NotFound(response);
        }

        return Ok(response);
    }
}
