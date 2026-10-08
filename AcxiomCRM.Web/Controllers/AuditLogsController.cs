using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.DTOs.Audit;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Enums;
using AcxiomCRM.Web.Services;

namespace AcxiomCRM.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
public class AuditLogsController : BaseController
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] AuditLogFilterDto filter)
    {
        var result = await _auditService.GetLogsAsync(filter);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> Export([FromQuery] AuditLogFilterDto filter)
    {
        filter.PageSize = 10000;
        var result = await _auditService.GetLogsAsync(filter);

        var sb = new StringBuilder();
        sb.AppendLine("LogId,Timestamp (UTC),User,Action,Entity,RecordId,OldValue,NewValue,IpAddress");

        foreach (var log in result.Items)
        {
            var oldClean = EscapeCsv(log.OldValue);
            var newClean = EscapeCsv(log.NewValue);
            sb.AppendLine($"{log.AuditLogId},{log.CreatedDate:yyyy-MM-dd HH:mm:ss},\"{log.UserName}\",\"{log.Action}\",\"{log.EntityName}\",\"{log.RecordId}\",\"{oldClean}\",\"{newClean}\",\"{log.IpAddress}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"AcxiomCRM_AuditLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
    }
}
