using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Reports;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class ReportsController : BaseController
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var stageReport = await _reportService.GetPipelineStageReportAsync(CurrentUserContext, null, null);
        var ownerReport = await _reportService.GetOwnerPipelineReportAsync(CurrentUserContext, null, null);
        var conversionReport = await _reportService.GetConversionReportAsync(CurrentUserContext, null, null);

        ViewBag.StageReport = stageReport;
        ViewBag.OwnerReport = ownerReport;
        ViewBag.ConversionReport = conversionReport;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Pipeline(DateTime? start, DateTime? end)
    {
        var stageReport = await _reportService.GetPipelineStageReportAsync(CurrentUserContext, start, end);
        var ownerReport = await _reportService.GetOwnerPipelineReportAsync(CurrentUserContext, start, end);

        ViewBag.StageReport = stageReport;
        ViewBag.OwnerReport = ownerReport;
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Conversion(DateTime? start, DateTime? end)
    {
        var report = await _reportService.GetConversionReportAsync(CurrentUserContext, start, end);
        ViewBag.Report = report;
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Activities(DateTime? start, DateTime? end)
    {
        var report = await _reportService.GetUserActivityReportAsync(CurrentUserContext, start, end);
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;

        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPipeline(DateTime? start, DateTime? end)
    {
        var stageReport = await _reportService.GetPipelineStageReportAsync(CurrentUserContext, start, end);
        var sb = new StringBuilder();
        sb.AppendLine("Stage,Opportunity Count,Total Amount ($),Weighted Amount ($)");

        foreach (var item in stageReport)
        {
            sb.AppendLine($"\"{item.StageName}\",{item.OpportunityCount},{item.TotalAmount:F2},{item.WeightedAmount:F2}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"AcxiomCRM_PipelineReport_{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}
