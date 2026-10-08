using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.DTOs.FollowUps;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class FollowUpsController : ControllerBase
{
    private readonly IFollowUpService _followUpService;
    private readonly ICurrentUserService _currentUserService;

    public FollowUpsController(IFollowUpService followUpService, ICurrentUserService currentUserService)
    {
        _followUpService = followUpService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<FollowUpDto>>> GetFollowUps([FromQuery] FollowUpFilterDto filter)
    {
        var result = await _followUpService.GetFollowUpsAsync(filter, _currentUserService.GetUserContext());
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FollowUpDto>>> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<FollowUpDto>.Fail("Validation failed.", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var response = await _followUpService.CreateFollowUpAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<ApiResponse<bool>>> CompleteFollowUp(int id, [FromBody] CompleteFollowUpDto dto)
    {
        if (id != dto.FollowUpId)
        {
            return BadRequest(ApiResponse<bool>.Fail("Route ID and payload ID do not match."));
        }

        var response = await _followUpService.CompleteFollowUpAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("{id:int}/reschedule")]
    public async Task<ActionResult<ApiResponse<bool>>> RescheduleFollowUp(int id, [FromBody] RescheduleFollowUpDto dto)
    {
        if (id != dto.FollowUpId)
        {
            return BadRequest(ApiResponse<bool>.Fail("Route ID and payload ID do not match."));
        }

        var response = await _followUpService.RescheduleFollowUpAsync(dto, _currentUserService.GetUserContext());
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
