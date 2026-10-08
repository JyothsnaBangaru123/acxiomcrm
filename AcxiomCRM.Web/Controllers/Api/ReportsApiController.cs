using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Reports;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[IgnoreAntiforgeryToken]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ICurrentUserService _currentUserService;

    public ReportsController(IReportService reportService, ICurrentUserService currentUserService)
    {
        _reportService = reportService;
        _currentUserService = currentUserService;
    }

    [HttpGet("pipeline")]
    public async Task<ActionResult<object>> GetPipelineReport([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var context = _currentUserService.GetUserContext();
        var stages = await _reportService.GetPipelineStageReportAsync(context, start, end);
        var owners = await _reportService.GetOwnerPipelineReportAsync(context, start, end);

        return Ok(new
        {
            stages,
            owners,
            totalPipeline = stages.Where(s => s.Stage != Domain.Enums.OpportunityStage.Lost).Sum(s => s.TotalAmount),
            totalWeighted = stages.Where(s => s.Stage != Domain.Enums.OpportunityStage.Lost).Sum(s => s.WeightedAmount)
        });
    }

    [HttpGet("conversion")]
    public async Task<ActionResult<ConversionRateReportDto>> GetConversionReport([FromQuery] DateTime? start, [FromQuery] DateTime? end)
    {
        var context = _currentUserService.GetUserContext();
        var report = await _reportService.GetConversionReportAsync(context, start, end);
        return Ok(report);
    }
}
