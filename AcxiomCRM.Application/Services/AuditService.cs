using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Application.DTOs.Audit;
using AcxiomCRM.Application.DTOs.Common;
using AcxiomCRM.Application.Interfaces;
using AcxiomCRM.Domain.Entities;
using AcxiomCRM.Domain.Interfaces;

namespace AcxiomCRM.Application.Services;

public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string entityName,
        string? recordId,
        string? oldValue = null,
        string? newValue = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = userId,
            UserName = userName ?? "System",
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            IpAddress = ipAddress ?? "127.0.0.1",
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.AuditLogs.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AuditLogs.Query();

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a => (a.UserName != null && a.UserName.ToLower().Contains(term))
                                  || a.Action.ToLower().Contains(term)
                                  || a.EntityName.ToLower().Contains(term)
                                  || (a.NewValue != null && a.NewValue.ToLower().Contains(term))
                                  || (a.OldValue != null && a.OldValue.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.UserId))
        {
            query = query.Where(a => a.UserId == filter.UserId);
        }

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
        {
            query = query.Where(a => a.EntityName == filter.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endOfDay = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new AuditLogDto
            {
                AuditLogId = a.AuditLogId,
                UserId = a.UserId,
                UserName = a.UserName,
                Action = a.Action,
                EntityName = a.EntityName,
                RecordId = a.RecordId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedDate = a.CreatedDate,
                IpAddress = a.IpAddress
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, totalCount, filter.PageNumber, filter.PageSize);
    }
}
