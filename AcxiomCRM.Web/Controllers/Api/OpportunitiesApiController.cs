using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.Opportunities;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;
    private readonly ICurrentUserService _currentUserService;

    public OpportunitiesController(IOpportunityService opportunityService, ICurrentUserService currentUserService)
    {
        _opportunityService = opportunityService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OpportunityDto>>> GetOpportunities([FromQuery] OpportunityFilterDto filter)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(filter, _currentUserService.GetUserContext());
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityDetailDto>> GetOpportunityById(int id)
    {
        var result = await _opportunityService.GetOpportunityDetailsAsync(id, _currentUserService.GetUserContext());
        if (result == null)
        {
            return NotFound(ApiResponse<OpportunityDetailDto>.Fail("Opportunity not found or unauthorized."));
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OpportunityDto>>> CreateOpportunity([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _opportunityService.CreateOpportunityAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetOpportunityById), new { id = response.Data!.OpportunityId }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<OpportunityDto>>> UpdateOpportunity(int id, [FromBody] UpdateOpportunityDto dto)
    {
        if (id != dto.OpportunityId)
        {
            return BadRequest(ApiResponse<OpportunityDto>.Fail("Route ID and payload ID do not match."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _opportunityService.UpdateOpportunityAsync(dto, _currentUserService.GetUserContext());
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
    public async Task<ActionResult<ApiResponse<bool>>> DeleteOpportunity(int id)
    {
        var response = await _opportunityService.DeleteOpportunityAsync(id, _currentUserService.GetUserContext());
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
