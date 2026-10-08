using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Leads;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly ICurrentUserService _currentUserService;

    public LeadsController(ILeadService leadService, ICurrentUserService currentUserService)
    {
        _leadService = leadService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadDto>>> GetLeads([FromQuery] LeadFilterDto filter)
    {
        var result = await _leadService.GetLeadsAsync(filter, _currentUserService.GetUserContext());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<LeadDetailDto>> GetLeadById(int id)
    {
        var result = await _leadService.GetLeadDetailsAsync(id, _currentUserService.GetUserContext());
        if (result == null)
        {
            return NotFound(ApiResponse<LeadDetailDto>.Fail("Lead not found or unauthorized."));
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<LeadDto>>> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<LeadDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _leadService.CreateLeadAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetLeadById), new { id = response.Data!.LeadId }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<LeadDto>>> UpdateLead(int id, [FromBody] UpdateLeadDto dto)
    {
        if (id != dto.LeadId)
        {
            return BadRequest(ApiResponse<LeadDto>.Fail("Route ID and payload ID do not match."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<LeadDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _leadService.UpdateLeadAsync(dto, _currentUserService.GetUserContext());
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

    [HttpPost("{id:int}/convert")]
    public async Task<ActionResult<ApiResponse<int>>> ConvertLead(int id, [FromBody] ConvertLeadDto dto)
    {
        if (id != dto.LeadId)
        {
            return BadRequest(ApiResponse<int>.Fail("Route ID and payload ID do not match."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<int>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _leadService.ConvertLeadAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLead(int id)
    {
        var response = await _leadService.DeleteLeadAsync(id, _currentUserService.GetUserContext());
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
